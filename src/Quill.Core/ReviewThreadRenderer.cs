using Quill.Core.Markdown;
using Quill.Core.Models;

namespace Quill.Core;

public static class ReviewThreadRenderer
{
    public static async Task<IReadOnlyList<PullRequestThreadResult>> RenderAsync(
        IEnumerable<PullRequestThread> threads,
        RichText richText,
        CancellationToken cancellationToken)
    {
        var results = new List<PullRequestThreadResult>();
        foreach (var thread in threads)
        {
            var commentResults = new List<PullRequestCommentResult>(thread.Comments.Count);
            foreach (var comment in thread.Comments)
            {
                var markdown = await richText.ToMarkdownAsync(comment.TextHtml, cancellationToken);

                commentResults.Add(PullRequestThreadResultBuilder.BuildComment(comment, markdown));
            }

            results.Add(PullRequestThreadResultBuilder.Build(thread, commentResults));
        }

        return results;
    }
}
