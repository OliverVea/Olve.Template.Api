using System.Text.Json.Serialization;
using Olve.MinimalApi;
using Olve.Results;
using Olve.Validation.Validators;

var builder = WebApplication.CreateSlimBuilder(args);
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonContext.Default));
builder.Services.WithPathJsonConversion();

var app = builder.Build();

app.MapGet("/health", () => Results.Ok());

app.MapGet("/echo", Result<string> (string? message) =>
{
    var result = new StringValidator()
        .CannotBeNullOrWhiteSpace()
        .WithMessage("'message' query parameter cannot be empty.")
        .Validate(message)
        .WithValueOnSuccess(message ?? string.Empty);

    return result;
}).WithResultMapping<string>();

app.Run();

[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(ResultProblem[]))]
internal partial class AppJsonContext : JsonSerializerContext;

public partial class Program;
