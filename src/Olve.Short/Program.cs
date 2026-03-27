using System.Text.Json.Serialization;

var builder = WebApplication.CreateSlimBuilder(args);
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonContext.Default));

var app = builder.Build();

app.MapGet("/health", () => Results.Ok());
app.MapGet("/echo", (HttpContext ctx) => Results.Ok(ctx.Request.Query["message"].ToString()));

app.Run();

[JsonSerializable(typeof(string))]
internal partial class AppJsonContext : JsonSerializerContext;
