using System.Globalization;
using Quill.Core.Models;

namespace Quill.Core;

public static class PullRequestResultBuilder
{
    public static PullRequestResult Build(PullRequest pullRequest, string currentUserId)
    {
        var matching = pullRequest.Reviewers.FirstOrDefault(
            r => string.Equals(r.Id, currentUserId, StringComparison.Ordinal));

        return new PullRequestResult
        {
            Id = pullRequest.Id,
            Title = pullRequest.Title,
            Author = pullRequest.AuthorDisplayName,
            State = pullRequest.Status,
            IsDraft = pullRequest.IsDraft,
            Repo = pullRequest.RepoName,
            Url = pullRequest.WebUrl,
            SourceBranch = pullRequest.SourceBranch,
            TargetBranch = pullRequest.TargetBranch,
            CreatedDate = FormatIsoUtc(pullRequest.CreatedDate),
            ClosedDate = pullRequest.ClosedDate is null ? null : FormatIsoUtc(pullRequest.ClosedDate.Value),
            MergeStatus = pullRequest.MergeStatus,
            Labels = pullRequest.Labels,
            Votes = PullRequestVotes.Count(pullRequest.Reviewers),
            MyVote = matching is null ? null : PullRequestVotes.Name(matching.Vote),
            MyIsRequired = matching?.IsRequired,
        };
    }

    private static string FormatIsoUtc(DateTimeOffset value)
    {
        return value.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);
    }
}
