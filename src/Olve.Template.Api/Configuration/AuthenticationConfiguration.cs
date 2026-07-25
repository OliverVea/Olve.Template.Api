using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Olve.Template.Api.Configuration;

public static class AuthenticationConfiguration
{
    public static void ConfigureAuthentication(this WebApplicationBuilder builder)
    {
        builder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                var authority = builder.Configuration["Auth:Authority"];
                var audience = builder.Configuration["Auth:Audience"];
                var signingKey = builder.Configuration["Auth:SigningKey"];
                var frontendAuthority = builder.Configuration["Auth:Frontend:Authority"];
                var frontendClientId = builder.Configuration["Auth:Frontend:ClientId"];

                options.Authority = authority;

                // The SPA logs in via its own PUBLIC Authentik provider, so its tokens carry that
                // provider's `iss` and an `aud` of its own client id — both different from the
                // resource provider's. Trust both issuers and both audiences (the SPA is this API's
                // own frontend). Both providers share Authentik's signing key, so the Authority
                // JWKS validates either signature; only the issuer/audience claims need widening.
                var validIssuers = new[] { authority, frontendAuthority }
                    .Where(v => !string.IsNullOrWhiteSpace(v))
                    .Distinct()
                    .ToArray();
                var validAudiences = new[] { audience, frontendClientId }
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
            });

        builder.Services.AddAuthorizationBuilder()
            .SetFallbackPolicy(
                new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build());
    }

    public static void MapAuthentication(this WebApplication app)
    {
        app.UseAuthentication();
        app.UseAuthorization();
    }
}
