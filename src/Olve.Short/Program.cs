using System.Text.Json.Serialization;
using Olve.MinimalApi;
using Olve.Results;
using Olve.Short.Echo;

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
builder.Services.AddOpenApi();
builder.Services.AddSingleton<EchoService>();

builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));

var host = builder.Configuration["Host"] ?? "localhost";
var port = builder.Configuration["Port"] ?? "5000";
builder.WebHost.UseUrls($"http://{host}:{port}");

var app = builder.Build();

app.MapOpenApi();

app.MapGet("/health", () => Results.Ok());

app.MapGet("/echo", (string? message, EchoService echo) => echo.Echo(message))
    .WithResultMapping<string>();

app.Run();

[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(Result<string>))]
internal partial class AppJsonContext : JsonSerializerContext;

public partial class Program;
