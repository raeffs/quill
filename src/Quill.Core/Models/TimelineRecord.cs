namespace Quill.Core.Models;

public class TimelineRecord
{
    public required string Id { get; init; }

    public string? ParentId { get; init; }

    public required string Type { get; init; }

    public required string Name { get; init; }

    public string? Identifier { get; init; }

    public required string State { get; init; }

    public string? Result { get; init; }

    public int? LogId { get; init; }

    public int ErrorCount { get; init; }

    public int WarningCount { get; init; }

    public int? Order { get; init; }

    public required IReadOnlyList<TimelineIssue> Issues { get; init; }
}

public class TimelineIssue
{
    public required string Type { get; init; }

    public required string Message { get; init; }

    public int? Line { get; init; }
}
