using Quill.Core.Markdown;
using Quill.Core.Models;

namespace Quill.Core.Actions.PullRequests;

public sealed record PrThreadsRequest(int Id, IReadOnlyList<string> Statuses, int? Limit);

public static class ThreadsAction
{
    public static async Task<IReadOnlyList<PullRequestThreadResult>> RunAsync(
        IAzureDevOpsPullRequestClient pullRequests,
        RichText richText,
        PrThreadsRequest request,
        CancellationToken cancellationToken)
    {
        var pullRequest = await pullRequests.GetByIdAsync(request.Id, cancellationToken);
        var threads = await pullRequests.GetThreadsAsync(request.Id, pullRequest.RepoName, cancellationToken);

        IEnumerable<PullRequestThread> filtered = threads;
        if (request.Statuses.Count > 0)
        {
            var statusSet = new HashSet<string>(request.Statuses, StringComparer.Ordinal);
            filtered = filtered.Where(t => statusSet.Contains(t.Status));
        }

        if (request.Limit is not null)
        {
            filtered = filtered.Take(request.Limit.Value);
        }

        return await ReviewThreadRenderer.RenderAsync(filtered, richText, cancellationToken);
    }
}
