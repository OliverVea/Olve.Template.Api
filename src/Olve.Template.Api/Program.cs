using Olve.Template.Api.Configuration;
using Olve.Template.Api.Health;
using Olve.Template.Api.Message;

var builder = WebApplication.CreateSlimBuilder(args);

builder.ConfigureHost(args);
builder.ConfigureJson();
builder.ConfigureAuthentication();
builder.ConfigureTelemetry();
builder.Services.AddMessageServices();

var app = builder.Build();

app.MapJson();
app.MapAuthentication();
app.MapHealthEndpoints();
app.MapMessageEndpoints();

app.Run();

public partial class Program;
