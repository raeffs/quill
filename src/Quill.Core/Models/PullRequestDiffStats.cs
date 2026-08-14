namespace Quill.Core.Models;

public class PullRequestDiffStats
{
    public required int TotalFiles { get; init; }

    public required int TotalAdded { get; init; }

    public required int TotalRemoved { get; init; }

    public required IReadOnlyList<PullRequestDiffFile> Files { get; init; }
}

public class PullRequestDiffFile
{
    public required string Path { get; init; }

    public required string ChangeType { get; init; }

    public string? OldPath { get; init; }

    public required int Added { get; init; }

    public required int Removed { get; init; }

    public bool Binary { get; init; }
}
