using Refit;

namespace Olve.Short.IntegrationTests;

public class EchoTests
{
    private static readonly AppFixture Fixture = new();

    [After(Class)]
    public static async Task Teardown() => await Fixture.DisposeAsync();

    [Test]
    public async Task Echo_ReturnsMessage()
    {
        var api = Fixture.CreateApiClient();

        var result = await api.Echo("hello");

        await Assert.That(result).IsEqualTo("\"hello\"");
    }

    [Test]
    public async Task Echo_EmptyMessage_ReturnsBadRequest()
    {
        var api = Fixture.CreateApiClient();

        var exception = await Assert.ThrowsAsync<ApiException>(async () => await api.Echo(""));

        await Assert.That(exception).IsNotNull();
        await Assert.That((int)exception!.StatusCode).IsEqualTo(400);
    }
}
