using Quill.Core.Models;

namespace Quill.Core;

public static class PullRequestThreadResultBuilder
{
    public static PullRequestThreadResult Build(PullRequestThread thread, IReadOnlyList<CommentResult> comments)
    {
        return new PullRequestThreadResult
        {
            Id = thread.Id,
            Status = thread.Status,
            FilePath = thread.FilePath,
            Side = thread.Side,
            StartLine = thread.StartLine,
            EndLine = thread.EndLine,
            Comments = comments,
        };
    }
}
