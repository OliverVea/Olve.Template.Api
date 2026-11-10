using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Olve.Template.Api.Configuration;

/// <summary>
/// Named tokens for machines (the pipeline's beta tests, another service): <c>Auth:Tokens:&lt;name&gt;</c> holds
/// the sha256 (hex) of a bearer token starting with <see cref="MachineTokenHandler.Prefix"/>. A token is allowed
/// like a listed user and acts as <c>&lt;name&gt;</c>. Dex has no client-credentials grant, so this is how a
/// machine gets in.
/// </summary>
public sealed class MachineTokenOptions : AuthenticationSchemeOptions
{
    public IReadOnlyDictionary<string, byte[]> Hashes { get; set; } = new Dictionary<string, byte[]>();

    public static IReadOnlyDictionary<string, byte[]> From(IConfiguration configuration) =>
        configuration.GetSection("Auth:Tokens").GetChildren()
            .Where(c => !string.IsNullOrWhiteSpace(c.Value))
            .ToDictionary(c => c.Key, c => Convert.FromHexString(c.Value!.Trim()), StringComparer.Ordinal);
}

public sealed class MachineTokenHandler(IOptionsMonitor<MachineTokenOptions> options, ILoggerFactory logger, UrlEncoder encoder)
    : AuthenticationHandler<MachineTokenOptions>(options, logger, encoder)
{
    public const string SchemeName = "MachineToken";
    public const string Prefix = "olve-template-api_";
    public const string NameClaim = "machine_token";

    public static bool Carries(HttpRequest request) =>
        request.Headers.Authorization.ToString() is var header
        && header.StartsWith("Bearer " + Prefix, StringComparison.Ordinal);

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Carries(Request))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(Request.Headers.Authorization.ToString()["Bearer ".Length..].Trim()));
        string? name = null;
        foreach (var (candidate, expected) in Options.Hashes)
        {
            if (CryptographicOperations.FixedTimeEquals(hash, expected))
            {
                name = candidate;
            }
        }

        if (name is null)
        {
            return Task.FromResult(AuthenticateResult.Fail("unknown token"));
        }

        var identity = new ClaimsIdentity(
            [new Claim("sub", $"token:{name}"), new Claim("preferred_username", name), new Claim(NameClaim, name)],
            SchemeName, "preferred_username", null);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName)));
    }
}
