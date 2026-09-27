using System.Net;

namespace Olve.Template.Api.ApiTests;

// Tests share one server (and, on a base-URL target, a live one), so they only assert on the
// messages they create themselves: never on counts or on the seeded welcome message.
[ClassDataSource<ApiTarget>(Shared = SharedType.PerTestSession)]
public class MessageTests(ApiTarget target)
{
    [Test]
    public async Task CreateMessage_Authenticated_ReturnsCreatedMessage()
    {
        using var client = target.CreateAuthenticatedClient();

        var created = await (await client.PostJsonAsync("/api/messages", new { text = "hello" })).ReadAsync<MessageBody>();

        await Assert.That(created.Text).IsEqualTo("hello");
        await Assert.That(created.Id).IsNotEqualTo(Guid.Empty);
    }

    [Test]
    public async Task CreateMessage_Unauthenticated_Returns401()
    {
        using var client = target.CreateClient();

        using var response = await client.PostJsonAsync("/api/messages", new { text = "hello" });

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task CreateMessage_EmptyText_Returns400()
    {
        using var client = target.CreateAuthenticatedClient();

        using var response = await client.PostJsonAsync("/api/messages", new { text = "" });

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task GetMessages_Anonymous_ReturnsPage()
    {
        using var client = target.CreateClient();

        using var response = await client.GetAsync("/api/messages?page=1&pageSize=1");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task GetMessages_AfterCreate_IncludesCreatedMessage()
    {
        using var client = target.CreateAuthenticatedClient();
        var created = await (await client.PostJsonAsync("/api/messages", new { text = "find-me" })).ReadAsync<MessageBody>();

        var found = await FindAsync(client, created.Id);

        await Assert.That(found?.Text).IsEqualTo("find-me");
    }

    [Test]
    public async Task UpdateMessage_ExistingMessage_ChangesText()
    {
        using var client = target.CreateAuthenticatedClient();
        var created = await (await client.PostJsonAsync("/api/messages", new { text = "before" })).ReadAsync<MessageBody>();

        var updated = await (await client.PutJsonAsync($"/api/messages/{created.Id}", new { text = "after" })).ReadAsync<MessageBody>();

        await Assert.That(updated.Id).IsEqualTo(created.Id);
        await Assert.That(updated.Text).IsEqualTo("after");
    }

    [Test]
    public async Task DeleteMessage_ExistingMessage_RemovesIt()
    {
        using var client = target.CreateAuthenticatedClient();
        var created = await (await client.PostJsonAsync("/api/messages", new { text = "delete-me" })).ReadAsync<MessageBody>();

        using var deleted = await client.DeleteAsync($"/api/messages/{created.Id}");

        await Assert.That(deleted.IsSuccessStatusCode).IsTrue();
        await Assert.That(await FindAsync(client, created.Id)).IsNull();
    }

    [Test]
    public async Task DeleteMessage_Unknown_Returns404()
    {
        using var client = target.CreateAuthenticatedClient();

        using var response = await client.DeleteAsync($"/api/messages/{Guid.NewGuid()}");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task UpdateMessage_Unknown_Returns404()
    {
        using var client = target.CreateAuthenticatedClient();

        using var response = await client.PutJsonAsync($"/api/messages/{Guid.NewGuid()}", new { text = "nobody home" });

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    // Pages through the whole list: on a shared server the message may not be on the first page.
    private static async Task<MessageBody?> FindAsync(HttpClient client, Guid id)
    {
        for (var page = 1; ; page++)
        {
            var body = await (await client.GetAsync($"/api/messages?page={page}&pageSize=100")).ReadAsync<MessagePageBody>();
            if (body.Items.FirstOrDefault(m => m.Id == id) is { } found)
            {
                return found;
            }

            if (page * 100 >= body.TotalCount)
            {
                return null;
            }
        }
    }
}
