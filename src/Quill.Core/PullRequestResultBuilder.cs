using System.Globalization;
using Quill.Core.Models;

namespace Quill.Core;

public static class PullRequestResultBuilder
{
    public static PullRequestResult Build(PullRequest pullRequest, string currentUserId)
    {
        var matching = pullRequest.Reviewers.FirstOrDefault(
            r => string.Equals(r.Id, currentUserId, StringComparison.Ordinal));

        var reviewers = new List<PullRequestReviewerResult>(pullRequest.Reviewers.Count);
        foreach (var r in pullRequest.Reviewers)
        {
            reviewers.Add(new PullRequestReviewerResult
            {
                DisplayName = r.DisplayName,
                Vote = r.Vote,
                IsRequired = r.IsRequired,
            });
        }

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
            Reviewers = reviewers,
            MyVote = matching?.Vote,
            MyIsRequired = matching?.IsRequired,
        };
    }

    private static string FormatIsoUtc(DateTimeOffset value)
    {
        return value.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);
    }
}
