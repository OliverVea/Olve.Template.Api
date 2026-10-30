using Olve.Template.Api.Contract;
using Olve.TypeSpec;

namespace Olve.Template.Api.Configuration;

[OperationHandler]
public sealed class AuthConfigHandler(IConfiguration config) : IAuthConfigGetHandler
{
    public Task<AuthConfigGetResponse> HandleAsync(AuthConfigGetRequest request, CancellationToken cancellationToken)
    {
        // Login runs against the SPA's own public provider (separate client from the confidential
        // one used for machine tokens). Fall back to the resource authority if no dedicated
        // frontend provider is configured (e.g. local dev).
        return Task.FromResult<AuthConfigGetResponse>(new FrontendAuthConfig
        {
            Authority = config["Auth:Frontend:Authority"] ?? config["Auth:Authority"],
            ClientId = config["Auth:Frontend:ClientId"],
            Scopes = config["Auth:Frontend:Scopes"] ?? "openid profile email offline_access",
        });
    }
}
