using Olve.Template.Api.Contract;
using Olve.TypeSpec;

namespace Olve.Template.Api.Health;

[OperationHandler]
public sealed class HealthHandler : IHealthGetHandler
{
    public Task<HealthGetResponse> HandleAsync(HealthGetRequest request, CancellationToken cancellationToken) =>
        Task.FromResult<HealthGetResponse>(new HealthGetResponse.Ok());
}
