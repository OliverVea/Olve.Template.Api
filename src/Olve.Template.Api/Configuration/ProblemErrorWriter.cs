using Olve.Results;
using Olve.Template.Api.Contract;
using Olve.TypeSpec;

namespace Olve.Template.Api.Configuration;

/// <summary>
/// Answers binding and validation failures with the contract's <see cref="Problem"/> array when the
/// operation declares the status, else with the status alone.
/// </summary>
public sealed class ProblemErrorWriter : IApiErrorWriter
{
    public IResult InvalidRequest(IReadOnlyList<ResultProblem> problems, int status, ApiOperation? operation) =>
        operation?.Declares(status) == true
            ? TypedResults.Json(problems.ToContract(), ServiceJsonContext.Default.IReadOnlyListProblem, statusCode: status)
            : TypedResults.StatusCode(status);
}

public static class ProblemMapping
{
    public static IReadOnlyList<Problem> ToContract(this IEnumerable<ResultProblem> problems) =>
        [.. problems.Select(p => new Problem
        {
            Message = p.FormattedMessage,
            Tags = p.Tags,
            Severity = p.Severity,
            IsRetryable = p.IsRetryable,
            Source = p.Source,
            ExceptionSummary = p.ExceptionSummary,
        })];
}
