using System.Net;
using System.Text.RegularExpressions;

namespace Olve.Template.Api.ApiTests;

/// <summary>
/// After a deploy a browser (an installed app above all) must load the new SPA: the page revalidates,
/// Vite's hashed bundle is cached for good. Skipped on a target that serves no SPA (in-process, or a
/// local <c>dotnet run</c>, where Vite serves it).
/// </summary>
[ClassDataSource<ApiTarget>(Shared = SharedType.PerTestSession)]
public partial class SpaCachingTests(ApiTarget target)
{
    private const string Revalidate = "no-cache";
    private const string Immutable = "public, max-age=31536000, immutable";

    [Test]
    [Arguments("/", Revalidate)]
    [Arguments("/index.html", Revalidate)]
    [Arguments("/messages/some-message", Revalidate)] // a client-side route, answered with the index
    [Arguments("/icons/icon-192.png", "public, max-age=86400")]
    public async Task SpaFile_Anonymous_HasCacheControl(string path, string cacheControl)
    {
        using var client = await SpaClientAsync();

        using var response = await client.GetAsync(path);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(response.Headers.CacheControl?.ToString()).IsEqualTo(cacheControl);
    }

    [Test]
    public async Task Bundle_Anonymous_IsImmutable()
    {
        using var client = await SpaClientAsync();
        var bundle = BundleSource().Match(await client.GetStringAsync("/")).Groups[1].Value;

        using var response = await client.GetAsync(bundle);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(response.Headers.CacheControl?.ToString()).IsEqualTo(Immutable);
    }

    // Every file in the image has the same mtime, so an ETag (mtime XOR length) can't tell a new
    // index.html from an old one of the same length; the page must never answer 304.
    [Test]
    public async Task Index_WithItsOwnETag_Returns200()
    {
        using var client = await SpaClientAsync();
        using var first = await client.GetAsync("/");
        using var again = new HttpRequestMessage(HttpMethod.Get, "/");
        if (first.Headers.ETag is { } etag)
        {
            again.Headers.IfNoneMatch.Add(etag);
        }
        if (first.Content.Headers.LastModified is { } lastModified)
        {
            again.Headers.IfModifiedSince = lastModified;
        }

        using var response = await client.SendAsync(again);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    private async Task<HttpClient> SpaClientAsync()
    {
        var client = target.CreateClient();
        using var index = await client.GetAsync("/");
        if (index.StatusCode != HttpStatusCode.OK) // 404, or 401 from the fallback policy in-process
        {
            client.Dispose();
            Skip.Test("The target serves no SPA.");
        }

        return client;
    }

    [GeneratedRegex(@"src=""(/assets/[^""]+\.js)""")]
    private static partial Regex BundleSource();
}
