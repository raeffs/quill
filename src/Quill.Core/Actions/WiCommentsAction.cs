using Microsoft.Extensions.Logging;
using Quill.Core.Markdown;
using Quill.Core.Models;

namespace Quill.Core.Actions;

public static class WiCommentsAction
{
    public static async Task<IReadOnlyList<CommentResult>> RunAsync(
        IAzureDevOpsClient client,
        QuillConfig config,
        ILogger logger,
        int id,
        int? limit,
        CancellationToken cancellationToken)
    {
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

        return results;
    }
}
