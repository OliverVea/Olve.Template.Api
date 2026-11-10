using Olve.Results;
using Olve.Template.Api.Contract;

namespace Olve.Template.Api.Configuration;

/// <summary>
/// What a request that matches no endpoint gets: under <c>/api</c>, 404 with a <c>Problem[]</c>
/// body; otherwise, for a GET that isn't a file, the SPA's index.html, so client-side routes
/// deep-link. It is middleware, not a fallback endpoint: a catch-all endpoint would also match
/// known API paths and swallow routing's own 405 (wrong method) and 415 (not JSON).
/// </summary>
public static class SpaConfiguration
{
    /// <summary>Call after the static files and before authentication: an unknown path needs no token.</summary>
    public static void UseSpa(this WebApplication app)
    {
        app.Use(async (context, next) =>
        {
            if (context.GetEndpoint() is not null)
            {
                await next(context);
                return;
            }

            var request = context.Request;
            if (request.Path.StartsWithSegments("/api"))
            {
                ResultProblem[] problems = [new("No API operation at {0} {1}.", request.Method, request.Path.Value!)];
                await TypedResults.Json(problems.ToContract(), ServiceJsonContext.Default.IReadOnlyListProblem, statusCode: StatusCodes.Status404NotFound)
                    .ExecuteAsync(context);
                return;
            }

            var index = app.Environment.WebRootFileProvider.GetFileInfo("index.html");
            var isFile = Path.HasExtension(request.Path.Value);
            if ((HttpMethods.IsGet(request.Method) || HttpMethods.IsHead(request.Method)) && !isFile && index.Exists)
            {
                context.Response.ContentType = "text/html; charset=utf-8";
                await context.Response.SendFileAsync(index);
                return;
            }

            await next(context); // 404; under `dotnet run` there is no index.html (Vite serves the SPA)
        });
    }
}
