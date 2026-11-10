using System.Net;

namespace Olve.Template.Api.ApiTests;

[ClassDataSource<ApiTarget>(Shared = SharedType.PerTestSession)]
public class RoutingTests(ApiTarget target)
{
    // An unknown /api path must not fall through to the SPA's index.html (a 200 with HTML).
    [Test]
    [Arguments("GET", "/api/nothing-here")]
    [Arguments("POST", "/api/nothing/here")]
    public async Task UnknownApiPath_Anonymous_Returns404WithProblem(string method, string path)
    {
        using var client = target.CreateClient();

        using var response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), path));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
        var problems = await response.ReadProblemsAsync();
        await Assert.That(problems.Single().Message).IsEqualTo($"No API operation at {method} {path}.");
    }

    // Routing's own answers aren't swallowed by the 404 for unknown paths.
    [Test]
    public async Task KnownPath_WrongMethod_Returns405()
    {
        using var client = target.CreateAuthenticatedClient();

        using var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Patch, "/api/messages"));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.MethodNotAllowed);
    }

    [Test]
    public async Task Create_NotJson_Returns415()
    {
        using var client = target.CreateAuthenticatedClient();

        using var response = await client.PostAsync("/api/messages", new StringContent("text", System.Text.Encoding.UTF8, "text/plain"));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.UnsupportedMediaType);
    }
}
