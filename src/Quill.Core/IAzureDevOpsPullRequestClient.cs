using Quill.Core.Models;

namespace Quill.Core;

public interface IAzureDevOpsPullRequestClient
{
    Task<IReadOnlyList<PullRequest>> ListAsync(
        string? creatorId,
        string? reviewerId,
        string status,
        string? repo,
        int top,
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

    Task<PullRequestDiffStats> GetDiffStatsAsync(
        int prId,
        string repo,
        CancellationToken cancellationToken = default);
}
