using Microsoft.Extensions.Logging;
using Quill.Core.Markdown;
using Quill.Core.Models;

namespace Quill.Core.Actions.Pr;

public static class ViewAction
{
    public static async Task<PullRequestViewResult> RunAsync(
        IAzureDevOpsPullRequestClient pullRequests,
        IAzureDevOpsClient workItems,
        IWorkItemBatchFetcher fetcher,
        IIdentityClient identity,
        QuillConfig config,
        ILogger logger,
        int id,
        bool withThreads,
        CancellationToken cancellationToken)
    {
        var currentUser = await identity.GetCurrentUserAsync();
        var pullRequest = await pullRequests.GetByIdAsync(id, cancellationToken);

        var description = string.IsNullOrEmpty(pullRequest.Description)
            ? string.Empty
            : (await MarkdownConverter.ToMarkdownAsync(
                pullRequest.Description, config.ServerUrl, config.Collection, config.Project, workItems, logger)).TrimEnd();

        var workItemIds = await pullRequests.GetWorkItemRefsAsync(id, pullRequest.RepoName, cancellationToken);

        var linkedWorkItems = await BuildLinkedWorkItemsAsync(fetcher, workItemIds, cancellationToken);

        IReadOnlyList<PullRequestThreadResult>? threads = null;
        if (withThreads)
        {
            var rawThreads = await pullRequests.GetThreadsAsync(id, pullRequest.RepoName, cancellationToken);
            threads = await ReviewThreadRenderer.RenderAsync(rawThreads, config, workItems, logger);
        }

        return PullRequestViewResultBuilder.Build(pullRequest, currentUser.Id, description, linkedWorkItems, threads);
    }

    private static async Task<IReadOnlyList<PullRequestLinkedWorkItemResult>> BuildLinkedWorkItemsAsync(
        IWorkItemBatchFetcher fetcher,
        IReadOnlyList<int> workItemIds,
        CancellationToken cancellationToken)
    {
        if (workItemIds.Count == 0)
        {
            return Array.Empty<PullRequestLinkedWorkItemResult>();
        }

        var batch = await fetcher.FetchAsync(workItemIds, cancellationToken);
        var itemsById = batch.Items.ToDictionary(i => i.Id);
        var batchFailed = new HashSet<int>(batch.BatchFailedIds);

        var results = new List<PullRequestLinkedWorkItemResult>(workItemIds.Count);
        foreach (var id in workItemIds)
        {
            if (itemsById.TryGetValue(id, out var workItem))
            {
                results.Add(PullRequestViewResultBuilder.BuildLinkedWorkItem(workItem));
            }
            else if (batchFailed.Contains(id))
            {
                results.Add(PullRequestViewResultBuilder.BuildErrorStub(id, "batch-failed"));
            }
            else
            {
                results.Add(PullRequestViewResultBuilder.BuildErrorStub(id, "unreadable"));
            }
        }

        return results;
    }
}
