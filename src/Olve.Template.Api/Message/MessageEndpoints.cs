using Olve.MinimalApi;

namespace Olve.Template.Api.Message;

public static class MessageEndpoints
{
    public static void AddMessageServices(this IServiceCollection services)
    {
        services.AddSingleton<MessageService>();
    }

    public static void MapMessageEndpoints(this WebApplication app)
    {
        app.MapGet("/message", (MessageService messages) => messages.Get())
            .WithResultMapping<string>()
            .AllowAnonymous();

        app.MapPost("/message", (MessageService messages, string? message) => messages.Set(message))
            .WithResultMapping<string>();
    }
}
