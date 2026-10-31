using Olve.Template.Api.Contract;
using Olve.TypeSpec;

namespace Olve.Template.Api.Configuration;

public static class ApiConfiguration
{
    /// <summary>The generated API, the handler of every contract operation and the error writer.</summary>
    public static void AddApi(this IServiceCollection services)
    {
        services.AddServiceApi().AddOperationHandlers();
        services.AddSingleton<IApiErrorWriter, ProblemErrorWriter>();
    }

    /// <summary>Maps the contract's operations. Everything needs a token (the fallback policy) unless opened here.</summary>
    public static void MapApi(this WebApplication app)
    {
        var api = app.UseServiceApi();
        api.HealthGet.AllowAnonymous();
        api.AuthConfigGet.AllowAnonymous();
        api.MessagesList.AllowAnonymous();
    }
}
