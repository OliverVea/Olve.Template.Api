using Olve.Results;
using Olve.Validation.Validators;

namespace Olve.Template.Api.Message;

public class MessageService(ILogger<MessageService> logger)
{
    private readonly Lock _lock = new();
    private string? _message;

    public Result<string> Get()
    {
        lock (_lock)
        {
            if (_message is null)
            {
                return new ResultProblem("No message has been set.");
            }

            return _message;
        }
    }

    public Result<string> Set(string? message)
    {
        var result = new StringValidator()
            .CannotBeNullOrWhiteSpace()
            .WithMessage("'message' cannot be empty.")
            .Validate(message)
            .WithValueOnSuccess(message ?? string.Empty);

        if (result.TryPickProblems(out _, out _))
        {
            logger.LogWarning("Message validation failed for: {Message}", message);
            return result;
        }

        lock (_lock)
        {
            _message = message;
        }

        return result;
    }
}
