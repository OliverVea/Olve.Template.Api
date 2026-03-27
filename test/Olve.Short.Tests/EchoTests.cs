using System.Net;

namespace Olve.Short.Tests;

public class EchoTests
{
    private static readonly HttpClient Http = new();
    private static readonly AppContainerFixture Fixture = new();

    [Before(Class)]
    public static async Task Setup() => await Fixture.StartAsync();

    [After(Class)]
    public static async Task Teardown() => await Fixture.DisposeAsync();

    [Test]
    public async Task Echo_ReturnsMessage()
    {
        var response = await Http.GetAsync($"{Fixture.BaseUrl}/echo?message=hello");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        await Assert.That(body).IsEqualTo("\"hello\"");
    }
}
