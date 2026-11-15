namespace Quill.Core.Models;

public class TimelineIssue
{
    public required string Type { get; init; }

    public required string Message { get; init; }

    public int? Line { get; init; }
}
