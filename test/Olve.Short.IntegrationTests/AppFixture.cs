using Microsoft.AspNetCore.Mvc.Testing;

namespace Olve.Short.IntegrationTests;

public class AppFixture : IAsyncDisposable
{
    private readonly WebApplicationFactory<Program> _factory = new();

    public HttpClient CreateClient() => _factory.CreateClient();

    public ValueTask DisposeAsync() => _factory.DisposeAsync();
}
