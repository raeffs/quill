using System.CommandLine;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quill.AzureDevOps;
using Quill.Core;
using Quill.Core.Markdown;
using Quill.Core.Models;

namespace Quill.Cli.Commands.Pr;

internal static class ViewCommand
{
    public static Command Create(IServiceProvider serviceProvider)
    {
        var idArg = new Argument<int>("id") { Description = "The Azure DevOps pull request ID to view" };

        var withThreadsOption = new Option<bool>("--with-threads")
        {
            Description = "Append a `threads` array with the PR's review threads (same payload as `pr threads`).",
        };

        var withDiffStatsOption = new Option<bool>("--with-diff-stats")
        {
            Description = "Append a `diffStats` object with per-file added/removed counts and aggregate totals.",
        };

        var command = new Command("view", "Print a pull request to stdout as JSON")
        {
            idArg,
            withThreadsOption,
            withDiffStatsOption,
        };

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var id = parseResult.GetValue(idArg);
            var withThreads = parseResult.GetValue(withThreadsOption);
            var withDiffStats = parseResult.GetValue(withDiffStatsOption);
            await ExecuteAsync(serviceProvider, id, withThreads, withDiffStats, cancellationToken);
        });

        return command;
    }

    private static async Task ExecuteAsync(
        IServiceProvider serviceProvider,
        int id,
        bool withThreads,
        bool withDiffStats,
        CancellationToken cancellationToken)
    {
        try
        {
            var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Quill.PrView");
            var config = serviceProvider.GetRequiredService<QuillConfig>();

            var identityClient = serviceProvider.GetRequiredService<AzureDevOpsIdentityClient>();
            var workItemClient = serviceProvider.GetRequiredService<AzureDevOpsClient>();
            var pullRequestClient = serviceProvider.GetRequiredService<AzureDevOpsPullRequestClient>();

            var currentUser = await identityClient.GetCurrentUserAsync();
            var pullRequest = await pullRequestClient.GetByIdAsync(id, cancellationToken);

            var description = string.IsNullOrEmpty(pullRequest.Description)
                ? string.Empty
                : (await MarkdownConverter.ToMarkdownAsync(
                    pullRequest.Description, config.ServerUrl, config.Collection, config.Project, workItemClient, logger)).TrimEnd();

            var workItemIds = await pullRequestClient.GetWorkItemRefsAsync(id, pullRequest.RepoName, cancellationToken);

            var workItems = await BuildLinkedWorkItemsAsync(workItemClient, workItemIds, cancellationToken);

            IReadOnlyList<PullRequestThreadResult>? threads = null;
            if (withThreads)
            {
                threads = await FetchThreadsAsync(
                    pullRequestClient, workItemClient, id, pullRequest.RepoName, config, logger, cancellationToken);
            }

            PullRequestDiffStats? diffStats = null;
            if (withDiffStats)
            {
                diffStats = await pullRequestClient.GetDiffStatsAsync(id, pullRequest.RepoName, cancellationToken);
            }

            var result = PullRequestViewResultBuilder.Build(
                pullRequest, currentUser.Id, description, workItems, threads, diffStats);

            Console.WriteLine(JsonSerializer.Serialize(result, CommandHelpers.Context.PullRequestViewResult));
        }
        catch (Exception ex) when (ex is FileNotFoundException or InvalidOperationException or HttpRequestException)
        {
            CommandHelpers.HandleError(ex);
        }
    }

    private static async Task<IReadOnlyList<PullRequestLinkedWorkItemResult>> BuildLinkedWorkItemsAsync(
        AzureDevOpsClient workItemClient,
        IReadOnlyList<int> workItemIds,
        CancellationToken cancellationToken)
    {
        if (workItemIds.Count == 0)
        {
            return Array.Empty<PullRequestLinkedWorkItemResult>();
        }

        var batch = await workItemClient.FetchAsync(workItemIds, cancellationToken);
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

    private static async Task<IReadOnlyList<PullRequestThreadResult>> FetchThreadsAsync(
        AzureDevOpsPullRequestClient pullRequestClient,
        AzureDevOpsClient workItemClient,
        int prId,
        string repo,
        QuillConfig config,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var threads = await pullRequestClient.GetThreadsAsync(prId, repo, cancellationToken);

        var results = new List<PullRequestThreadResult>(threads.Count);
        foreach (var thread in threads)
        {
            var commentResults = new List<CommentResult>(thread.Comments.Count);
            foreach (var comment in thread.Comments)
            {
                var markdown = string.IsNullOrEmpty(comment.TextHtml)
                    ? string.Empty
                    : (await MarkdownConverter.ToMarkdownAsync(
                        comment.TextHtml, config.ServerUrl, config.Collection, config.Project, workItemClient, logger)).TrimEnd();

                commentResults.Add(CommentsResultBuilder.Build(comment, markdown));
            }

            results.Add(PullRequestThreadResultBuilder.Build(thread, commentResults));
        }

        return results;
    }
}
