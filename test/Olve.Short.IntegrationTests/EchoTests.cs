using System.Net;
using System.Net.Http.Json;

namespace Olve.Short.IntegrationTests;

public class EchoTests
{
    private static readonly AppFixture Fixture = new();

    [After(Class)]
    public static async Task Teardown() => await Fixture.DisposeAsync();

    [Test]
    public async Task Echo_ReturnsMessage()
    {
        using var client = Fixture.CreateClient();

        var response = await client.GetAsync("/echo?message=hello");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        await Assert.That(body).IsEqualTo("\"hello\"");
    }

    [Test]
    public async Task Echo_EmptyMessage_ReturnsBadRequest()
    {
        using var client = Fixture.CreateClient();

        var response = await client.GetAsync("/echo");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);

        var problems = await response.Content.ReadFromJsonAsync<ProblemDto[]>();
        await Assert.That(problems).IsNotNull();
        await Assert.That(problems!).HasSingleItem();
        await Assert.That(problems![0].Message).IsEqualTo("'message' query parameter cannot be empty.");
    }

    private record ProblemDto(string Message);
}
