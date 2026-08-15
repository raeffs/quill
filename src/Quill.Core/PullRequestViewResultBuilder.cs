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
        IReadOnlyList<PullRequestThreadResult>? threads,
        PullRequestDiffStats? diffStats)
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
            Labels = pullRequest.Labels,
            Votes = PullRequestVotes.Count(pullRequest.Reviewers),
            Reviewers = reviewers,
            MyVote = matching is null ? null : PullRequestVotes.Name(matching.Vote),
            MyIsRequired = matching?.IsRequired,
            Description = markdownDescription,
            WorkItems = workItems,
            Threads = threads,
            DiffStats = diffStats is null ? null : BuildDiffStats(diffStats),
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

    private static PullRequestDiffStatsResult BuildDiffStats(PullRequestDiffStats stats)
    {
        var files = new List<PullRequestDiffFileResult>(stats.Files.Count);
        foreach (var f in stats.Files)
        {
            files.Add(new PullRequestDiffFileResult
            {
                Path = f.Path,
                ChangeType = f.ChangeType,
                OldPath = f.OldPath,
                Added = f.Added,
                Removed = f.Removed,
                Binary = f.Binary,
            });
        }

        return new PullRequestDiffStatsResult
        {
            TotalFiles = stats.TotalFiles,
            TotalAdded = stats.TotalAdded,
            TotalRemoved = stats.TotalRemoved,
            Files = files,
        };
    }

    private static string FormatIsoUtc(DateTimeOffset value)
    {
        return value.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);
    }
}
