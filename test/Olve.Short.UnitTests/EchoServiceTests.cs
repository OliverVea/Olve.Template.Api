using Microsoft.Extensions.Logging;
using Olve.Short.Echo;
using Rocks;

[assembly: Rock(typeof(ILogger<>), BuildType.Make)]

namespace Olve.Short.UnitTests;

public class EchoServiceTests
{
    private readonly EchoService _sut;

    public EchoServiceTests()
    {
        var mock = new ILoggerMakeExpectations<EchoService>().Instance();
        _sut = new EchoService(mock);
    }

    [Test]
    public async Task Echo_ValidMessage_ReturnsMessage()
    {
        var result = _sut.Echo("hello");

        await Assert.That(result.TryPickValue(out var value, out _)).IsTrue();
        await Assert.That(value).IsEqualTo("hello");
    }

    [Test]
    public async Task Echo_NullMessage_ReturnsProblems()
    {
        var result = _sut.Echo(null);

        await Assert.That(result.TryPickProblems(out _, out _)).IsTrue();
    }

    [Test]
    public async Task Echo_EmptyMessage_ReturnsProblems()
    {
        var result = _sut.Echo("");

        await Assert.That(result.TryPickProblems(out _, out _)).IsTrue();
    }

    [Test]
    public async Task Echo_WhitespaceMessage_ReturnsProblems()
    {
        var result = _sut.Echo("   ");

        await Assert.That(result.TryPickProblems(out _, out _)).IsTrue();
    }
}
