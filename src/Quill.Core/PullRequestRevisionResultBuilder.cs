using System.Globalization;
using Quill.Core.Models;

namespace Quill.Core;

public static class PullRequestRevisionResultBuilder
{
    public static PullRequestRevisionResult Build(PullRequestRevision revision)
    {
        ArgumentNullException.ThrowIfNull(revision);

        return new PullRequestRevisionResult
        {
            Id = revision.Id,
            CreatedDate = FormatIsoUtc(revision.CreatedDate),
            Author = revision.Author,
            SourceCommit = revision.SourceCommit,
            TargetCommit = revision.TargetCommit,
            CommonCommit = revision.CommonCommit,
        };
    }

    private static string FormatIsoUtc(DateTimeOffset value)
    {
        return value.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);
    }
}
