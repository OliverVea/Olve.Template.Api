using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace Olve.Short.Tests;

public class AppContainerFixture : IAsyncDisposable
{
    private readonly IContainer _container;

    public string BaseUrl => $"http://localhost:{_container.GetMappedPublicPort(8080)}";

    public AppContainerFixture()
    {
        _container = new ContainerBuilder("olve-short:latest")
            .WithPortBinding(8080, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(r => r.ForPort(8080).ForPath("/health")))
            .Build();
    }

    public Task StartAsync() => _container.StartAsync();

    public ValueTask DisposeAsync() => _container.DisposeAsync();
}
