using System.Net;

namespace Olve.Template.Api.IntegrationTests;

[ClassDataSource<AppFixture>(Shared = SharedType.PerAssembly)]
public class MessageTests(AppFixture fixture)
{
    [Test]
    public async Task PostMessage_Authenticated_ReturnsMessage()
    {
        var api = fixture.CreateApiClient();

        var result = await api.MessagePost("hello");

        await Assert.That(result).IsEqualTo("\"hello\"");
    }

    [Test]
    public async Task PostMessage_Unauthenticated_Returns401()
    {
        var client = fixture.CreateUnauthenticatedHttpClient();

        var response = await client.PostAsync("/message?message=hello", null);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task GetMessage_AfterPost_ReturnsMessage()
    {
        var api = fixture.CreateApiClient();

        await api.MessagePost("hello");
        var result = await api.MessageGet();

        await Assert.That(result).IsEqualTo("\"hello\"");
    }
}
