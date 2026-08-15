using System.Globalization;
using Quill.Core.Models;

namespace Quill.Core;

public static class PullRequestViewResultBuilder
{
    public static PullRequestViewResult Build(
        PullRequest pullRequest,
        string currentUserId,
        string markdownDescription,
        IReadOnlyList<PullRequestLinkedWorkItemResult> workItems,
        IReadOnlyList<PullRequestThreadResult>? threads)
    {
        var matching = pullRequest.Reviewers.FirstOrDefault(
            r => string.Equals(r.Id, currentUserId, StringComparison.Ordinal));

        var reviewers = new List<PullRequestReviewerResult>(pullRequest.Reviewers.Count);
        foreach (var r in pullRequest.Reviewers)
        {
            reviewers.Add(new PullRequestReviewerResult
            {
                DisplayName = r.DisplayName,
                Vote = PullRequestVotes.Name(r.Vote),
                IsRequired = r.IsRequired,
                IsContainer = r.IsContainer,
            });
        }

        return new PullRequestViewResult
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
            LastMergeSourceCommit = pullRequest.LastMergeSourceCommit,
            LastMergeTargetCommit = pullRequest.LastMergeTargetCommit,
            Labels = pullRequest.Labels,
            Votes = PullRequestVotes.Count(pullRequest.Reviewers),
            Reviewers = reviewers,
            MyVote = matching is null ? null : PullRequestVotes.Name(matching.Vote),
            MyIsRequired = matching?.IsRequired,
            Description = markdownDescription,
            WorkItems = workItems,
            Threads = threads,
        };
    }

    public static PullRequestLinkedWorkItemResult BuildLinkedWorkItem(WorkItem workItem)
    {
        return new PullRequestLinkedWorkItemResult
        {
            Id = workItem.Id,
            Title = workItem.Title,
            State = workItem.State,
            Type = workItem.Type,
            AssignedTo = string.IsNullOrEmpty(workItem.AssignedToDisplayName) ? null : workItem.AssignedToDisplayName,
            ParentId = workItem.ParentId,
        };
    }

    public static PullRequestLinkedWorkItemResult BuildErrorStub(int id, string error)
    {
        return new PullRequestLinkedWorkItemResult
        {
            Id = id,
            Error = error,
        };
    }

    private static string FormatIsoUtc(DateTimeOffset value)
    {
        return value.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);
    }
}
