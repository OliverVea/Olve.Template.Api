using Microsoft.AspNetCore.Mvc.Testing;
using Olve.Short.Client;
using Refit;

namespace Olve.Short.IntegrationTests;

public class AppFixture : IAsyncDisposable
{
    private readonly WebApplicationFactory<Program> _factory = new();

    public IOlveShortv1 CreateApiClient() =>
        RestService.For<IOlveShortv1>(_factory.CreateClient());

    public ValueTask DisposeAsync() => _factory.DisposeAsync();
}
