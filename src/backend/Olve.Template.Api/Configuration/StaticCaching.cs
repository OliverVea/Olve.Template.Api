using Microsoft.Net.Http.Headers;

namespace Olve.Template.Api.Configuration;

/// <summary>
/// How long a browser, or a proxy in front of the app, may keep the SPA's files. The image's files all
/// carry a 1970 mtime, so without an explicit policy a browser treats a cached index.html as fresh for
/// years and keeps loading an old bundle (an installed app worst of all). Vite's content-hashed
/// <c>/assets</c> never change; the icons may be a day old; the service worker and the manifest are
/// never stored, so no cache between the browser and the app can hold back an update; everything else
/// (index.html, and the index served for client-side routes) revalidates on every load.
/// </summary>
public static class StaticCaching
{
    public const string Revalidate = "no-cache";
    public const string NoStore = "no-store";
    public const string Immutable = "public, max-age=31536000, immutable";
    public const string OneDay = "public, max-age=86400";

    public static string CacheControlFor(PathString path) =>
        path.StartsWithSegments("/assets") ? Immutable
        : path.StartsWithSegments("/icons") ? OneDay
        : path == "/sw.js" || path == "/manifest.webmanifest" ? NoStore
        : Revalidate;

    public static StaticFileOptions Options() => new()
    {
        OnPrepareResponse = context => context.Context.Response.Headers[HeaderNames.CacheControl] =
            CacheControlFor(context.Context.Request.Path),
    };

    /// <summary>
    /// Call before the static files. A static file's ETag is its mtime XOR its length, so with every
    /// mtime at 1970 a new index.html of the same length (Vite's hashes are fixed-length) would match
    /// the old one's ETag and answer 304 with the stale page. Files that aren't cached for good ignore
    /// the browser's validators and always answer 200; they are small.
    /// </summary>
    public static void UseStaticCaching(this WebApplication app)
    {
        app.Use((context, next) =>
        {
            var request = context.Request;
            if (!request.Path.StartsWithSegments("/api") && CacheControlFor(request.Path) is Revalidate or NoStore)
            {
                request.Headers.Remove(HeaderNames.IfNoneMatch);
                request.Headers.Remove(HeaderNames.IfModifiedSince);
            }

            return next(context);
        });
    }
}
