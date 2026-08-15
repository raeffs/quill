using System.Globalization;
using Quill.Core.Models;

namespace Quill.Core;

public static class PullRequestThreadResultBuilder
{
    private const string IsoUtcFormat = "yyyy-MM-ddTHH:mm:ssZ";

    public static PullRequestThreadResult Build(
        PullRequestThread thread,
        IReadOnlyList<PullRequestCommentResult> comments)
    {
        return new PullRequestThreadResult
        {
            Id = thread.Id,
            Status = thread.Status,
            FilePath = thread.FilePath,
            Side = thread.Side,
            StartLine = thread.StartLine,
            EndLine = thread.EndLine,
            PositionState = thread.PositionState,
            OrigFilePath = thread.OrigFilePath,
            OrigStartLine = thread.OrigStartLine,
            OrigEndLine = thread.OrigEndLine,
            OrigStartColumn = thread.OrigStartColumn,
            OrigEndColumn = thread.OrigEndColumn,
            PublishedDate = FormatUtc(thread.PublishedDate),
            LastUpdatedDate = FormatUtc(thread.LastUpdatedDate),
            Comments = comments,
        };
    }

    public static PullRequestCommentResult BuildComment(PullRequestComment comment, string markdownText)
    {
        return new PullRequestCommentResult
        {
            Id = comment.Id,
            Author = comment.Author,
            CreatedDate = FormatUtc(comment.CreatedDate),
            ModifiedDate = comment.ModifiedDate is { } m ? FormatUtc(m) : null,
            LastUpdatedDate = FormatUtc(comment.LastUpdatedDate),
            UsersLiked = comment.UsersLiked,
            Text = markdownText,
        };
    }

    private static string FormatUtc(DateTimeOffset value) =>
        value.UtcDateTime.ToString(IsoUtcFormat, CultureInfo.InvariantCulture);
}
