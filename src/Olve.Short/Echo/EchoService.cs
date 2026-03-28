using Olve.Results;
using Olve.Validation.Validators;

namespace Olve.Short.Echo;

public class EchoService(ILogger<EchoService> logger)
{
    public Result<string> Echo(string? message)
    {
        var result = new StringValidator()
            .CannotBeNullOrWhiteSpace()
            .WithMessage("'message' query parameter cannot be empty.")
            .Validate(message)
            .WithValueOnSuccess(message ?? string.Empty);

        if (result.TryPickProblems(out _, out _))
        {
            logger.LogWarning("Echo validation failed for message: {Message}", message);
        }

        return result;
    }
}
