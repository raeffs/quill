using System.Globalization;
using Quill.Core.Models;

namespace Quill.Core;

public static class CommentsResultBuilder
{
    private const string IsoUtcFormat = "yyyy-MM-ddTHH:mm:ssZ";

    public static CommentResult Build(WorkItemComment comment, string markdownText)
    {
        return new CommentResult
        {
            Id = comment.Id,
            Author = comment.Author,
            CreatedDate = FormatUtc(comment.CreatedDate),
            ModifiedDate = comment.ModifiedDate is { } m ? FormatUtc(m) : null,
            Text = markdownText,
        };
    }

    private static string FormatUtc(DateTimeOffset value) =>
        value.UtcDateTime.ToString(IsoUtcFormat, CultureInfo.InvariantCulture);
}
