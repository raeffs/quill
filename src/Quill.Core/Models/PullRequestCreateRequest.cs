namespace Quill.Core.Models;

public class PullRequestCreateRequest
{
    public required string Repo { get; init; }

    public required string SourceBranch { get; init; }

    public required string Title { get; init; }

    public string? TargetBranch { get; init; }

    public string? Description { get; init; }

    public IReadOnlyList<int> WorkItemIds { get; init; } = [];
}
