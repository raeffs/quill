namespace Quill.Core.Models;

public class PullRequestListQuery
{
    public string? CreatorId { get; init; }

    public string? ReviewerId { get; init; }

    public required string Status { get; init; }

    public string? Repo { get; init; }

    public string? SourceBranch { get; init; }

    public string? TargetBranch { get; init; }

    public required int Top { get; init; }

    public int? Skip { get; init; }
}
