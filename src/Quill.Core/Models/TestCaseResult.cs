namespace Quill.Core.Models;

public class TestCaseResult
{
    public string? Title { get; init; }

    public string? Name { get; init; }

    public string? Storage { get; init; }

    public required string Outcome { get; init; }

    public string? Message { get; init; }

    public string? StackTrace { get; init; }

    public double? DurationInMs { get; init; }

    public DateTimeOffset? FailingSince { get; init; }

    public int? FailingSinceRunId { get; init; }
}
