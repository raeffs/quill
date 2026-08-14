using Quill.Core.Models;

namespace Quill.Core;

public interface IWorkItemBatchFetcher
{
    Task<BatchFetchResult> FetchAsync(IReadOnlyList<int> ids, CancellationToken cancellationToken = default);
}
