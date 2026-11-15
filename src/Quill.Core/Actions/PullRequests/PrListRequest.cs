namespace Quill.Core.Actions.PullRequests;

public sealed record PrListRequest(
    string? Reviewer,
    string? Author,
    string State,
    string? Repo,
    string? SourceBranch,
    string? TargetBranch,
    int Limit,
    int? Skip);
