using Microsoft.Extensions.Logging;
using Olve.Template.Api.Message;
using Rocks;

namespace Olve.Template.Api.UnitTests;

public class MessageServiceTests
{
    private readonly MessageService _sut;

    public MessageServiceTests()
    {
        var mock = new ILoggerMakeExpectations<MessageService>().Instance();
        _sut = new MessageService(mock);
    }

    [Test]
    public async Task Get_NoMessageSet_ReturnsProblems()
    {
        var result = _sut.Get();

        await Assert.That(result.TryPickProblems(out _, out _)).IsTrue();
    }

    [Test]
    public async Task Set_ValidMessage_ReturnsMessage()
    {
        var result = _sut.Set("hello");

        await Assert.That(result.TryPickValue(out var value, out _)).IsTrue();
        await Assert.That(value).IsEqualTo("hello");
    }

    [Test]
    public async Task Set_ThenGet_ReturnsSetMessage()
    {
        _sut.Set("hello");
        var result = _sut.Get();

        await Assert.That(result.TryPickValue(out var value, out _)).IsTrue();
        await Assert.That(value).IsEqualTo("hello");
    }

    [Test]
    public async Task Set_NullMessage_ReturnsProblems()
    {
        var result = _sut.Set(null);

        await Assert.That(result.TryPickProblems(out _, out _)).IsTrue();
    }

    [Test]
    public async Task Set_EmptyMessage_ReturnsProblems()
    {
        var result = _sut.Set("");

        await Assert.That(result.TryPickProblems(out _, out _)).IsTrue();
    }

    [Test]
    public async Task Set_WhitespaceMessage_ReturnsProblems()
    {
        var result = _sut.Set("   ");

        await Assert.That(result.TryPickProblems(out _, out _)).IsTrue();
    }
}
