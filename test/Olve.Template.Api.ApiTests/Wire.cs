using System.Net.Http.Json;
using System.Text.Json;

namespace Olve.Template.Api.ApiTests;

/// <summary>
/// The wire shapes the tests read and write. Deliberately hand-written rather than shared with the
/// service: a renamed or retyped field in the service must fail these tests, not silently follow.
/// </summary>
public sealed record MessageBody(Guid Id, string Text);

public sealed record MessagePageBody(IReadOnlyList<MessageBody> Items, int TotalCount);

public sealed record AuthConfigBody(string? Authority, string? ClientId, string Scopes);

public static class Wire
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static async Task<T> ReadAsync<T>(this HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<T>(Options))!;
    }

    public static Task<HttpResponseMessage> PostJsonAsync(this HttpClient client, string path, object body) =>
        client.PostAsJsonAsync(path, body, Options);

    public static Task<HttpResponseMessage> PutJsonAsync(this HttpClient client, string path, object body) =>
        client.PutAsJsonAsync(path, body, Options);
}
