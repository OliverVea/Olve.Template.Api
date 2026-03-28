using Microsoft.AspNetCore.Mvc.Testing;
using Olve.Template.Api.Client;
using Refit;

namespace Olve.Template.Api.IntegrationTests;

public class AppFixture : IAsyncDisposable
{
    private readonly WebApplicationFactory<Program> _factory = new();

    public IOlveTemplateApiv1 CreateApiClient() =>
        RestService.For<IOlveTemplateApiv1>(_factory.CreateClient());

    public ValueTask DisposeAsync() => _factory.DisposeAsync();
}
