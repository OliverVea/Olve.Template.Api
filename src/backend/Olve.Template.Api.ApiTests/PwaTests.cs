using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Olve.Template.Api.ApiTests;

/// <summary>
/// The SPA is an installable web app: the page links a valid manifest whose icons are served, and the
/// service worker is served at the root with nothing allowed to store it, so a deploy reaches every
/// browser. Skipped on a target that serves no SPA (in-process, or a local <c>dotnet run</c>, where <c>/</c> answers 401 or 404).
/// </summary>
[ClassDataSource<ApiTarget>(Shared = SharedType.PerTestSession)]
public partial class PwaTests(ApiTarget target)
{
    [Test]
    public async Task Index_Anonymous_LinksTheManifest()
    {
        using var client = await SpaClientAsync();

        var index = await client.GetStringAsync("/");

        await Assert.That(ManifestLink().Match(index).Groups[1].Value).IsEqualTo("/manifest.webmanifest");
    }

    [Test]
    public async Task Manifest_Anonymous_IsInstallable()
    {
        using var client = await SpaClientAsync();

        using var response = await client.GetAsync("/manifest.webmanifest");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(response.Content.Headers.ContentType?.MediaType).IsEqualTo("application/manifest+json");
        using var manifest = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = manifest.RootElement;
        await Assert.That(root.GetProperty("name").GetString()).IsNotNullOrEmpty();
        await Assert.That(root.GetProperty("short_name").GetString()).IsNotNullOrEmpty();
        await Assert.That(root.GetProperty("display").GetString()).IsEqualTo("standalone");
        await Assert.That(root.GetProperty("start_url").GetString()).IsEqualTo("/");
        await Assert.That(root.GetProperty("scope").GetString()).IsEqualTo("/");
        await Assert.That(root.GetProperty("theme_color").GetString()).IsNotNullOrEmpty();
        await Assert.That(root.GetProperty("background_color").GetString()).IsNotNullOrEmpty();

        var icons = root.GetProperty("icons").EnumerateArray().ToList();
        string[] sizes = [.. icons.Select(i => i.GetProperty("sizes").GetString()!)];
        await Assert.That(sizes).Contains("192x192");
        await Assert.That(sizes).Contains("512x512");
        await Assert.That(icons.Any(i => i.TryGetProperty("purpose", out var p) && p.GetString() == "maskable")).IsTrue();
        foreach (var icon in icons)
        {
            using var image = await client.GetAsync(icon.GetProperty("src").GetString());
            await Assert.That(image.StatusCode).IsEqualTo(HttpStatusCode.OK);
            await Assert.That(image.Content.Headers.ContentType?.MediaType).IsEqualTo(icon.GetProperty("type").GetString());
        }
    }

    [Test]
    public async Task AppleTouchIcon_Anonymous_IsServed()
    {
        using var client = await SpaClientAsync();

        using var response = await client.GetAsync("/icons/apple-touch-icon.png");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(response.Content.Headers.ContentType?.MediaType).IsEqualTo("image/png");
    }

    // A browser checks /sw.js for a new build; a cached copy anywhere on the way would hold back the update.
    [Test]
    [Arguments("/sw.js", "text/javascript")]
    [Arguments("/manifest.webmanifest", "application/manifest+json")]
    public async Task UpdateFile_Anonymous_IsNeverStored(string path, string mediaType)
    {
        using var client = await SpaClientAsync();

        using var response = await client.GetAsync(path);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(response.Content.Headers.ContentType?.MediaType).IsEqualTo(mediaType);
        await Assert.That(response.Headers.CacheControl?.ToString()).IsEqualTo("no-store");
    }

    [Test]
    public async Task ServiceWorker_Anonymous_PrecachesTheBuildAndNoApi()
    {
        using var client = await SpaClientAsync();
        var index = await client.GetStringAsync("/");
        var bundle = BundleSource().Match(index).Groups[1].Value;

        var worker = await client.GetStringAsync("/sw.js");

        var precache = JsonSerializer.Deserialize<string[]>(Precache().Match(worker).Groups[1].Value)!;
        await Assert.That(precache).Contains("/index.html");
        await Assert.That(precache).Contains(bundle);
        await Assert.That(precache.Any(p => p.StartsWith("/api") || p.StartsWith("/dex") || p.StartsWith("/callback"))).IsFalse();
        await Assert.That(Version().Match(worker).Success).IsTrue();
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

    [GeneratedRegex(@"<link rel=""manifest"" href=""([^""]+)""")]
    private static partial Regex ManifestLink();

    [GeneratedRegex(@"src=""(/assets/[^""]+\.js)""")]
    private static partial Regex BundleSource();

    [GeneratedRegex(@"^const PRECACHE = (\[.*\]);$", RegexOptions.Multiline)]
    private static partial Regex Precache();

    [GeneratedRegex(@"^const VERSION = ""[0-9a-f]{12}"";$", RegexOptions.Multiline)]
    private static partial Regex Version();
}
