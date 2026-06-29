using Olve.Template.Api.Configuration;
using Olve.Template.Api.Health;
using Olve.Template.Api.Messages;
using Olve.Utilities.AsyncOnStartup;

var builder = WebApplication.CreateSlimBuilder(args);

builder.ConfigureHost(args);
builder.ConfigureJson();
builder.ConfigureAuthentication();
builder.ConfigureTelemetry();
builder.Services.AddMessageServices(builder.Configuration);

var app = builder.Build();

app.MapJson();
app.MapAuthentication();
app.MapHealthEndpoints();
app.MapMessageEndpoints();

// Start the host (the persister loads its snapshot here), then run one-shot startup tasks
// against the populated stores, then block until shutdown.
await app.StartAsync();
await app.Services.RunAsyncOnStartup();
await app.WaitForShutdownAsync();

public partial class Program;
