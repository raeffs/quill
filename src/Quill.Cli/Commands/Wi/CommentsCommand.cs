using System.CommandLine;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quill.AzureDevOps;
using Quill.Core;
using Quill.Core.Markdown;
using Quill.Core.Models;

namespace Quill.Cli.Commands.Wi;

internal static class CommentsCommand
{
    public static Command Create(IServiceProvider serviceProvider)
    {
        var idArg = new Argument<int>("id") { Description = "The Azure DevOps work item ID whose comment thread to read" };

        var limitOption = new Option<int?>("--limit")
        {
            Description = "Return only the N most recent comments (must be >= 1). Omit to return all comments.",
        };

        var command = new Command("comments", "Print the comment thread on a work item as JSON, newest first")
        {
            idArg,
            limitOption,
        };

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var id = parseResult.GetValue(idArg);
            var limit = parseResult.GetValue(limitOption);
            await ExecuteAsync(serviceProvider, id, limit, cancellationToken);
        });

        return command;
    }

    private static async Task ExecuteAsync(IServiceProvider serviceProvider, int id, int? limit, CancellationToken cancellationToken)
    {
        try
        {
            if (limit is not null && limit.Value < 1)
            {
                throw new InvalidOperationException("--limit must be at least 1");
            }

            var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Quill.Comments");
            var config = serviceProvider.GetRequiredService<QuillConfig>();

            var client = serviceProvider.GetRequiredService<AzureDevOpsClient>();

            var comments = await client.GetCommentsAsync(id, limit, cancellationToken);

            var results = new List<CommentResult>(comments.Count);
            foreach (var comment in comments)
            {
                var markdown = string.IsNullOrEmpty(comment.TextHtml)
                    ? string.Empty
                    : (await MarkdownConverter.ToMarkdownAsync(
                        comment.TextHtml, config.ServerUrl, config.Collection, config.Project, client, logger)).TrimEnd();

                results.Add(CommentsResultBuilder.Build(comment, markdown));
            }

            Console.WriteLine(JsonSerializer.Serialize(results, CommandHelpers.Context.ListCommentResult));
        }
        catch (Exception ex) when (ex is FileNotFoundException or InvalidOperationException or HttpRequestException)
        {
            CommandHelpers.HandleError(ex);
        }
    }
}
