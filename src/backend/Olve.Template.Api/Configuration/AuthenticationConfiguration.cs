using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;

namespace Olve.Template.Api.Configuration;

public static class AuthenticationConfiguration
{
    public static void ConfigureAuthentication(this WebApplicationBuilder builder)
    {
        // Auth:Disable (the chart's auth.mode none): every endpoint is anonymous. Local or trusted networks only.
        if (builder.Configuration.GetValue<bool>("Auth:Disable"))
        {
            builder.Services.AddAuthentication().AddJwtBearer();
            builder.Services.AddAuthorization();
            return;
        }

        const string selector = "Bearer or machine token";
        var tokens = MachineTokenOptions.From(builder.Configuration);
        builder.Services
            .AddAuthentication(selector)
            .AddPolicyScheme(selector, null, o => o.ForwardDefaultSelector = http =>
                MachineTokenHandler.Carries(http.Request) ? MachineTokenHandler.SchemeName : JwtBearerDefaults.AuthenticationScheme)
            .AddScheme<MachineTokenOptions, MachineTokenHandler>(MachineTokenHandler.SchemeName, o => o.Hashes = tokens)
            .AddJwtBearer(options =>
            {
                var authority = builder.Configuration["Auth:Authority"];
                var audience = builder.Configuration["Auth:Audience"];
                var signingKey = builder.Configuration["Auth:SigningKey"];
                var frontendAuthority = builder.Configuration["Auth:FrontendAuthority"];
                var frontendAudience = builder.Configuration["Auth:FrontendAudience"];

                options.Authority = authority;

                // The SPA may log in as its own client (with Dex: a second static client; with Authentik:
                // a second, public provider), so its tokens can carry another `iss` and `aud`. Trust both:
                // the SPA is this API's own frontend.
                var validIssuers = new[] { authority, frontendAuthority }
                    .Where(v => !string.IsNullOrWhiteSpace(v))
                    .Distinct()
                    .ToArray();
                var validAudiences = new[] { audience, frontendAudience }
                    .Where(v => !string.IsNullOrWhiteSpace(v))
                    .Distinct()
                    .ToArray();

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = validIssuers.Length > 0,
                    ValidIssuers = validIssuers.Length > 0 ? validIssuers : null,
                    ValidateAudience = validAudiences.Length > 0,
                    ValidAudiences = validAudiences.Length > 0 ? validAudiences : null,
                    ValidateLifetime = true,
                };

                // A plain-HTTP OIDC provider (a local Dex) needs Auth:RequireHttpsMetadata=false.
                if (!builder.Configuration.GetValue("Auth:RequireHttpsMetadata", true))
                {
                    options.RequireHttpsMetadata = false;
                }

                if (builder.Environment.IsDevelopment())
                {
                    options.RequireHttpsMetadata = false;
                    options.BackchannelHttpHandler = new HttpClientHandler
                    {
                        ServerCertificateCustomValidationCallback =
                            HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
                    };
                }

                if (signingKey is not null)
                {
                    options.Authority = null;
                    options.RequireHttpsMetadata = false;
                    options.TokenValidationParameters.IssuerSigningKey =
                        new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));
                    options.TokenValidationParameters.ValidateIssuerSigningKey = true;
                }

                options.Events = DiagnosticEvents(validIssuers, validAudiences);
            });

        var allow = AuthAllowList.From(builder.Configuration);
        var signedIn = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .RequireAssertion(ctx => ctx.User.HasClaim(c => c.Type == MachineTokenHandler.NameClaim) || allow.Allows(ctx.User) || Refused(ctx))
            .Build();

        builder.Services.AddAuthorizationBuilder()
            .SetDefaultPolicy(signedIn)
            .SetFallbackPolicy(signedIn);
    }

    // A valid token outside Auth:Allow answers 403. Logs the stable subject id, so an admin can put it in
    // Auth:Allow:Subjects.
    private static bool Refused(AuthorizationHandlerContext ctx)
    {
        if (ctx.User.Identity?.IsAuthenticated == true && ctx.Resource is HttpContext http)
        {
            http.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("Olve.Template.Api.Auth")
                .LogWarning("Refused '{Username}' (subject '{Subject}'): not in Auth:Allow",
                    ctx.User.FindFirst("preferred_username")?.Value, AuthAllowList.Subject(ctx.User));
        }

        return false;
    }

    // Diagnostics for the SPA login: a rejected bearer is otherwise a silent 401. Failures log at Warning
    // (visible everywhere) with the IDX reason + the token's iss/aud/kid so a mismatch against
    // ValidIssuers/ValidAudiences is obvious; the happy path logs at Debug. Never the token.
    private static JwtBearerEvents DiagnosticEvents(string?[] validIssuers, string?[] validAudiences) => new()
    {
        OnAuthenticationFailed = context =>
        {
            var logger = context.HttpContext.RequestServices
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger("Olve.Template.Api.Auth");
            var (iss, aud, kid) = DescribeToken(context.Request);
            logger.LogWarning(context.Exception,
                "JWT auth failed: {Reason} | token iss={Iss} aud={Aud} kid={Kid} | expected iss=[{ValidIssuers}] aud=[{ValidAudiences}]",
                context.Exception.Message, iss, aud, kid,
                string.Join(", ", validIssuers), string.Join(", ", validAudiences));
            return Task.CompletedTask;
        },
        OnChallenge = context =>
        {
            var logger = context.HttpContext.RequestServices
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger("Olve.Template.Api.Auth");
            logger.LogDebug(
                "JWT challenge on {Path}: error={Error} desc={Desc}",
                context.Request.Path, context.Error, context.ErrorDescription);
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            var logger = context.HttpContext.RequestServices
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger("Olve.Template.Api.Auth");
            var principal = context.Principal;
            logger.LogDebug("JWT validated: iss={Iss} aud={Aud} sub={Sub}",
                principal?.FindFirst("iss")?.Value,
                string.Join(",", principal?.FindAll("aud").Select(c => c.Value) ?? []),
                principal?.FindFirst("sub")?.Value);
            return Task.CompletedTask;
        },
    };

    public static void MapAuthentication(this WebApplication app)
    {
        app.UseAuthentication();
        app.UseAuthorization();
    }

    // Read iss/aud/kid from the *incoming* bearer without validating it — so a rejected token's
    // claims are visible in the failure log next to what the API expected. Best-effort: any parse
    // problem yields nulls rather than throwing inside the auth event.
    private static (string? Iss, string? Aud, string? Kid) DescribeToken(HttpRequest request)
    {
        var header = request.Headers.Authorization.ToString();
        if (string.IsNullOrEmpty(header) ||
            !header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return (null, null, null);
        }

        var parts = header["Bearer ".Length..].Trim().Split('.');
        if (parts.Length < 2)
        {
            return (null, null, null);
        }

        try
        {
            return (ReadClaim(parts[1], "iss"), ReadClaim(parts[1], "aud"), ReadClaim(parts[0], "kid"));
        }
        catch
        {
            return (null, null, null);
        }
    }

    private static string? ReadClaim(string segment, string name)
    {
        using var doc = JsonDocument.Parse(Base64UrlDecode(segment));
        if (!doc.RootElement.TryGetProperty(name, out var el))
        {
            return null;
        }

        return el.ValueKind switch
        {
            JsonValueKind.String => el.GetString(),
            JsonValueKind.Array => string.Join(",", el.EnumerateArray().Select(x => x.GetString())),
            _ => el.ToString(),
        };
    }

    private static byte[] Base64UrlDecode(string input)
    {
        var s = input.Replace('-', '+').Replace('_', '/');
        s += (s.Length % 4) switch { 2 => "==", 3 => "=", _ => "" };
        return Convert.FromBase64String(s);
    }
}
