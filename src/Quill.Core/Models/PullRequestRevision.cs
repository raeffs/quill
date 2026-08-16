namespace Quill.Core.Models;

/// <summary>One recorded state of a pull request's source branch.</summary>
public class PullRequestRevision
{
    public required int Id { get; init; }

    public required DateTimeOffset CreatedDate { get; init; }

    public string? Author { get; init; }

    /// <summary>Source branch head at this revision.</summary>
    public string? SourceCommit { get; init; }

    /// <summary>Target branch head at this revision. Not the diff base.</summary>
    public string? TargetCommit { get; init; }

    /// <summary>The merge base, and the only correct diff base for this revision.</summary>
    public string? CommonCommit { get; init; }
}
