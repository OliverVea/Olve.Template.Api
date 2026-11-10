using Microsoft.Extensions.Logging.Console;

namespace Olve.Template.Api.Configuration;

public static class LoggingConfiguration
{
    /// <summary>
    /// Makes <c>Logging:Console:FormatterName</c> take effect. <c>CreateSlimBuilder</c> pins the console
    /// formatter to <c>simple</c> after the config binding, so a deployed <c>FormatterName=json</c> would be
    /// ignored; re-applying the configured name last wins. Unset (local dev), the simple formatter stays.
    /// </summary>
    public static void ConfigureLogging(this WebApplicationBuilder builder)
        => builder.Logging.AddConfiguredConsoleFormatter(builder.Configuration);

    public static ILoggingBuilder AddConfiguredConsoleFormatter(this ILoggingBuilder logging, IConfiguration configuration)
    {
        logging.AddConsole();
        logging.Services.Configure<ConsoleLoggerOptions>(options =>
        {
            if (configuration["Logging:Console:FormatterName"] is { Length: > 0 } name)
                options.FormatterName = name;
        });
        // Request log lines carry the hosting scope's TraceId.
        logging.Services.Configure<JsonConsoleFormatterOptions>(o => o.IncludeScopes = true);
        logging.Services.Configure<SimpleConsoleFormatterOptions>(o => o.IncludeScopes = true);
        return logging;
    }
}
