namespace Quill.Core.Models;

public sealed class BatchFetchResult
{
    public required IReadOnlyList<WorkItem> Items { get; init; }

    public required IReadOnlyList<int> BatchFailedIds { get; init; }
}
