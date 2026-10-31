using System.Net;

namespace Olve.Template.Api.ApiTests;

[ClassDataSource<ApiTarget>(Shared = SharedType.PerTestSession)]
public class HealthTests(ApiTarget target)
{
    [Test]
    public async Task Health_Anonymous_ReturnsOk()
    {
        using var client = target.CreateClient();

        using var response = await client.GetAsync("/health");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task AuthConfig_Anonymous_ReturnsScopes()
    {
        using var client = target.CreateClient();

        var config = await (await client.GetAsync("/api/auth-config")).ReadAsync<AuthConfigBody>();

        await Assert.That(config.Scopes).Contains("openid");
    }
}
