namespace Olve.Template.Api.Configuration;

public static class HostConfiguration
{
    public static void ConfigureHost(this WebApplicationBuilder builder, string[] args)
    {
        var environment = builder.Environment.EnvironmentName;

        builder.Configuration.Sources.Clear();
        builder.Configuration
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .AddEnvironmentVariables()
            .AddJsonFile("appsettings.local.json", optional: true)
            .AddCommandLine(args);

        builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));

        var host = builder.Configuration["Host"] ?? "localhost";
        var port = builder.Configuration["Port"] ?? "5000";
        builder.WebHost.UseUrls($"http://{host}:{port}");
    }
}
