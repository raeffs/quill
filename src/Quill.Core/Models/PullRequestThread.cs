namespace Quill.Core.Models;

public class PullRequestThread
{
    public required int Id { get; init; }

    public required string Status { get; init; }

    public required DateTimeOffset PublishedDate { get; init; }

    public required DateTimeOffset LastUpdatedDate { get; init; }

    public string? FilePath { get; init; }

    public string? Side { get; init; }

    /// <summary>The current position: where the commented code sits at the latest revision.</summary>
    public int? StartLine { get; init; }

    public int? EndLine { get; init; }

    /// <summary>How far the current position can be trusted. Null on a thread with no anchor.</summary>
    public string? PositionState { get; init; }

    /// <summary>Set only when the file was renamed after the reviewer commented.</summary>
    public string? OrigFilePath { get; init; }

    /// <summary>The original position: the anchor as the reviewer left it.</summary>
    public int? OrigStartLine { get; init; }

    public int? OrigEndLine { get; init; }

    public int? OrigStartColumn { get; init; }

    public int? OrigEndColumn { get; init; }

    public required IReadOnlyList<PullRequestComment> Comments { get; init; }
}
