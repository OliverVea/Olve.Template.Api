using System.Net;

namespace Olve.Template.Api.IntegrationTests;

public class MessageTests
{
    private static readonly AppFixture Fixture = new();

    [After(Class)]
    public static async Task Teardown() => await Fixture.DisposeAsync();

    [Test]
    public async Task PostMessage_Authenticated_ReturnsMessage()
    {
        var api = Fixture.CreateApiClient();

        var result = await api.MessagePOST("hello");

        await Assert.That(result).IsEqualTo("\"hello\"");
    }

    [Test]
    public async Task PostMessage_Unauthenticated_Returns401()
    {
        var client = Fixture.CreateUnauthenticatedHttpClient();

        var response = await client.PostAsync("/message?message=hello", null);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task GetMessage_AfterPost_ReturnsMessage()
    {
        var api = Fixture.CreateApiClient();

        await api.MessagePOST("hello");
        var result = await api.MessageGET();

        await Assert.That(result).IsEqualTo("\"hello\"");
    }
}
