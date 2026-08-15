using Quill.Core.Models;

namespace Quill.Core;

public interface IAzureDevOpsPullRequestClient
{
    Task<IReadOnlyList<PullRequest>> ListAsync(
        PullRequestListQuery query,
        CancellationToken cancellationToken = default);

    Task<PullRequest> GetByIdAsync(int prId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PullRequestThread>> GetThreadsAsync(
        int prId,
        string repo,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<int>> GetWorkItemRefsAsync(
        int prId,
        string repo,
        CancellationToken cancellationToken = default);
}
