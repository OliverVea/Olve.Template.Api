using System.Net;

namespace Olve.Template.Api.ApiTests;

[ClassDataSource<ApiTarget>(Shared = SharedType.PerTestSession)]
public class RoutingTests(ApiTarget target)
{
    // An unknown /api path must not fall through to the SPA's index.html (a 200 with HTML).
    [Test]
    [Arguments("GET", "/api/nothing-here")]
    [Arguments("POST", "/api/nothing/here")]
    [Arguments("GET", "/api/messages/00000000-0000-0000-0000-000000000001")] // the contract has no get-by-id
    public async Task UnknownApiPath_Anonymous_Returns404WithProblem(string method, string path)
    {
        using var client = target.CreateClient();

        using var response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), path));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
        var problems = await response.ReadProblemsAsync();
        await Assert.That(problems.Single().Message).IsEqualTo($"No API operation at {method} {path}.");
    }
}
