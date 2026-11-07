using Microsoft.Extensions.Logging;
using Quill.Core.Markdown;
using Quill.Core.Models;

namespace Quill.Core;

public static class ReviewThreadRenderer
{
    public static async Task<IReadOnlyList<PullRequestThreadResult>> RenderAsync(
        IEnumerable<PullRequestThread> threads,
        QuillConfig config,
        IAzureDevOpsClient workItems,
        ILogger logger)
    {
        var results = new List<PullRequestThreadResult>();
        foreach (var thread in threads)
        {
            var commentResults = new List<PullRequestCommentResult>(thread.Comments.Count);
            foreach (var comment in thread.Comments)
            {
                var markdown = string.IsNullOrEmpty(comment.TextHtml)
                    ? string.Empty
                    : (await MarkdownConverter.ToMarkdownAsync(
                        comment.TextHtml, config.ServerUrl, config.Collection, config.Project, workItems, logger)).TrimEnd();

                commentResults.Add(PullRequestThreadResultBuilder.BuildComment(comment, markdown));
            }

            results.Add(PullRequestThreadResultBuilder.Build(thread, commentResults));
        }

        return results;
    }
}
