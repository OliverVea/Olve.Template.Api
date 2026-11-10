using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using Olve.Template.Api.Configuration;

namespace Olve.Template.Api.UnitTests.Configuration;

// The node collector parses JSON log lines into queryable fields; under CreateSlimBuilder's pinned
// "simple" formatter, FormatterName=json would silently stay plain text.
public class LoggingJsonFormatTests
{
    private sealed class FormatterLogger(string category, ConsoleFormatter formatter, TextWriter writer) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> format)
            => formatter.Write(new LogEntry<TState>(logLevel, category, eventId, state, exception, format), null, writer);
    }

    [Test]
    public async Task FormatterNameJson_WinsOverTheSlimBuilderPin_AndArgsAreFields()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Logging:Console:FormatterName"] = "json" })
            .Build();

        using var provider = new ServiceCollection()
            .AddLogging(b =>
            {
                b.AddSimpleConsole(); // what CreateSlimBuilder registers
                b.AddConfiguredConsoleFormatter(config);
            })
            .BuildServiceProvider();

        var name = provider.GetRequiredService<IOptionsMonitor<ConsoleLoggerOptions>>().CurrentValue.FormatterName;
        var formatter = provider.GetRequiredService<IEnumerable<ConsoleFormatter>>().Single(f => f.Name == name);
        var buffer = new StringWriter();
        new FormatterLogger("Test", formatter, buffer).LogInformation("Message {MessageId} created", "abc123");

        var root = JsonDocument.Parse(buffer.ToString().Trim()).RootElement;
        await Assert.That(root.GetProperty("Message").GetString()).IsEqualTo("Message abc123 created");
        await Assert.That(root.GetProperty("State").GetProperty("MessageId").GetString()).IsEqualTo("abc123");
    }
}
