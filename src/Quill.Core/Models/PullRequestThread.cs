namespace Quill.Core.Models;

public class PullRequestThread
{
    public required int Id { get; init; }

    public required string Status { get; init; }

    public required DateTimeOffset PublishedDate { get; init; }

    public string? FilePath { get; init; }

    public string? Side { get; init; }

    public int? StartLine { get; init; }

    public int? EndLine { get; init; }

    public required IReadOnlyList<WorkItemComment> Comments { get; init; }
}
