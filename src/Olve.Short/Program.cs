using System.Text.Json.Serialization;
using Olve.MinimalApi;
using Olve.Results;
using Olve.Validation.Validators;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Configuration.Sources.Clear();
builder.Configuration
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables()
    .AddJsonFile("appsettings.local.json", optional: true)
    .AddCommandLine(args);

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonContext.Default));
builder.Services.WithPathJsonConversion();

builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));

var host = builder.Configuration["Host"] ?? "localhost";
var port = builder.Configuration["Port"] ?? "5000";
builder.WebHost.UseUrls($"http://{host}:{port}");

var app = builder.Build();

var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Olve.Short");

app.MapGet("/health", () => Results.Ok());

app.MapGet("/echo", Result<string> (string? message) =>
{
    var result = new StringValidator()
        .CannotBeNullOrWhiteSpace()
        .WithMessage("'message' query parameter cannot be empty.")
        .Validate(message)
        .WithValueOnSuccess(message ?? string.Empty);

    if (result.TryPickProblems(out _, out _))
    {
        logger.LogWarning("Echo validation failed for message: {Message}", message);
    }

    return result;
}).WithResultMapping<string>();

app.Run();

[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(Result<string>))]
internal partial class AppJsonContext : JsonSerializerContext;

public partial class Program;
