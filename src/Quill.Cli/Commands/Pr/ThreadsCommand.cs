using System.CommandLine;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quill.AzureDevOps;
using Quill.Core;
using Quill.Core.Markdown;
using Quill.Core.Models;

namespace Quill.Cli.Commands.Pr;

internal static class ThreadsCommand
{
    public static Command Create(IServiceProvider serviceProvider)
    {
        var idArg = new Argument<int>("id") { Description = "The Azure DevOps pull request ID whose threads to read" };

        var statusOption = new Option<string[]>("--status")
        {
            Description = "Filter by thread status (repeat to OR). Accepts active, fixed, wontFix, closed, pending, byDesign. Omit to return all.",
            AllowMultipleArgumentsPerToken = false,
        };

        var limitOption = new Option<int?>("--limit")
        {
            Description = "Return only the N most recent threads (must be >= 1). Omit to return all threads.",
        };

        var command = new Command("threads", "Print a pull request's review threads as JSON, newest first")
        {
            idArg,
            statusOption,
            limitOption,
        };

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var id = parseResult.GetValue(idArg);
            var status = parseResult.GetValue(statusOption) ?? Array.Empty<string>();
            var limit = parseResult.GetValue(limitOption);
            await ExecuteAsync(serviceProvider, id, status, limit, cancellationToken);
        });

        return command;
    }

    private static async Task ExecuteAsync(
        IServiceProvider serviceProvider,
        int id,
        string[] statusFilters,
        int? limit,
        CancellationToken cancellationToken)
    {
        try
        {
            if (limit is not null && limit.Value < 1)
            {
                throw new InvalidOperationException("--limit must be at least 1");
            }

            var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Quill.PrThreads");
            var config = serviceProvider.GetRequiredService<QuillConfig>();

            var workItemClient = serviceProvider.GetRequiredService<AzureDevOpsClient>();
            var pullRequestClient = serviceProvider.GetRequiredService<AzureDevOpsPullRequestClient>();

            var pullRequest = await pullRequestClient.GetByIdAsync(id, cancellationToken);
            var threads = await pullRequestClient.GetThreadsAsync(id, pullRequest.RepoName, cancellationToken);

            IEnumerable<PullRequestThread> filtered = threads;
            if (statusFilters.Length > 0)
            {
                var statusSet = new HashSet<string>(statusFilters, StringComparer.Ordinal);
                filtered = filtered.Where(t => statusSet.Contains(t.Status));
            }

            if (limit is not null)
            {
                filtered = filtered.Take(limit.Value);
            }

            var results = new List<PullRequestThreadResult>();
            foreach (var thread in filtered)
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

            Console.WriteLine(JsonSerializer.Serialize(results, CommandHelpers.Context.ListPullRequestThreadResult));
        }
        catch (Exception ex) when (ex is FileNotFoundException or InvalidOperationException or HttpRequestException)
        {
            CommandHelpers.HandleError(ex);
        }
    }
}
