using Olve.Results;
using Olve.Template.Api.Configuration;
using Olve.Template.Api.Contract;
using Olve.Template.Api.Messages;
using Olve.Utilities.AsyncOnStartup;
using Olve.Utilities.Stores;

var builder = WebApplication.CreateSlimBuilder(args);

builder.ConfigureHost(args);
builder.ConfigureLogging();
builder.ConfigureAuthentication();
builder.ConfigureTelemetry();
builder.Services.AddApi();
builder.Services.AddMessageServices(builder.Configuration);

var app = builder.Build();

// Store event handlers are isolated from each other; without this their exceptions are swallowed.
var eventLogger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Olve.Template.Api.Events");
EventDispatch.OnHandlerException = ex => eventLogger.LogError(ex, "A store event handler threw.");

// Serve the SPA (src/frontend/dist, copied into wwwroot by the Dockerfile) at the site root.
// Static assets and the index fallback are anonymous — the RequireAuthenticatedUser fallback
// policy would otherwise 401 them. In local dev the SPA is served by Vite (`bun run dev`),
// so wwwroot is only populated in the container and this no-ops when running `dotnet run`.
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapAuthentication();
app.MapApi();

// An unmatched /api path is a 404 with a Problem[] body, not the SPA's index.html. Anonymous: the
// fallback policy would answer 401 instead, hiding that the operation doesn't exist.
app.MapFallback("/api/{**path}", context =>
        TypedResults.Json(
                new[] { new ResultProblem("No API operation at {0} {1}.", context.Request.Method, context.Request.Path.Value ?? "") }.ToContract(),
                ServiceJsonContext.Default.IReadOnlyListProblem,
                statusCode: StatusCodes.Status404NotFound)
            .ExecuteAsync(context))
    .AllowAnonymous();

// SPA client-side routing: any other unmatched path returns index.html so deep links work.
app.MapFallbackToFile("index.html").AllowAnonymous();

// Start the host (the persister loads its snapshot here), then run one-shot startup tasks
// against the populated stores, then block until shutdown.
await app.StartAsync();
await app.Services.RunAsyncOnStartup();
await app.WaitForShutdownAsync();

public partial class Program;
