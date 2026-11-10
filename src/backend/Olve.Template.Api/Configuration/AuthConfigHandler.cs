using Olve.Template.Api.Contract;
using Olve.TypeSpec;

namespace Olve.Template.Api.Configuration;

[OperationHandler]
public sealed class AuthConfigHandler(IConfiguration config) : IAuthConfigGetHandler
{
    /// <summary>
    /// Space-separated scopes to request: <c>offline_access</c> for the refresh token, and <c>groups</c> only when
    /// <c>Auth:Allow:Orgs</c> needs the claim. <c>Auth:FrontendScopes</c> replaces them.
    /// </summary>
    public static string Scopes(AuthAllowList allow) =>
        allow.Orgs.Count > 0 ? "openid profile email offline_access groups" : "openid profile email offline_access";

    public Task<AuthConfigGetResponse> HandleAsync(AuthConfigGetRequest request, CancellationToken cancellationToken)
    {
        // The SPA logs in as its frontend client when one is configured, else as the API's own client.
        return Task.FromResult<AuthConfigGetResponse>(new FrontendAuthConfig
        {
            Authority = config["Auth:FrontendAuthority"] ?? config["Auth:Authority"],
            ClientId = config["Auth:FrontendAudience"] ?? config["Auth:Audience"],
            Scopes = config["Auth:FrontendScopes"] ?? Scopes(AuthAllowList.From(config)),
        });
    }
}
