namespace Quill.Core.Models;

public class PullRequest
{
    public required int Id { get; init; }

    public required string Title { get; init; }

    public required string AuthorDisplayName { get; init; }

    public required string Status { get; init; }

    public required bool IsDraft { get; init; }

    public required string RepoName { get; init; }

    public required string SourceBranch { get; init; }

    public required string TargetBranch { get; init; }

    public required DateTimeOffset CreatedDate { get; init; }

    public DateTimeOffset? ClosedDate { get; init; }

    public required IReadOnlyList<PullRequestReviewer> Reviewers { get; init; }

    public string? MergeStatus { get; init; }

    public IReadOnlyList<string> Labels { get; init; } = Array.Empty<string>();

    public required string WebUrl { get; init; }

    public string Description { get; init; } = string.Empty;
}
