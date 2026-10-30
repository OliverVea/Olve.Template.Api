using Olve.Results.TUnit;
using Olve.Template.Api.Contract;
using MessageRequestValidator = Olve.Template.Api.Messages.MessageRequestValidator;

namespace Olve.Template.Api.UnitTests.Messages;

public class MessageRequestValidatorTests
{
    [Test]
    public async Task Validate_NonEmptyText_Succeeds()
    {
        var result = MessageRequestValidator.Validate(new MessageRequest { Text = "hello" });

        await Assert.That(result).Succeeded();
    }

    [Test]
    [Arguments("")]
    [Arguments("   ")]
    public async Task Validate_EmptyOrWhitespace_Fails(string text)
    {
        var result = MessageRequestValidator.Validate(new MessageRequest { Text = text });

        await Assert.That(result).Failed();
    }
}
