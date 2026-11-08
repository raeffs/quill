using System.Globalization;
using Quill.Core.Models;

namespace Quill.Core;

public static class PullRequestChecksResultBuilder
{
    public static PullRequestChecksResult Build(PullRequest pullRequest, IReadOnlyList<PolicyEvaluation> evaluations)
    {
        return new PullRequestChecksResult
        {
            Id = pullRequest.Id,
            Verdict = Judge(pullRequest, evaluations),
            Status = pullRequest.Status,
            MergeStatus = pullRequest.MergeStatus,
            IsDraft = pullRequest.IsDraft,
            Checks = evaluations.Select(BuildCheck).ToList(),
        };
    }

    private static string? Judge(PullRequest pullRequest, IReadOnlyList<PolicyEvaluation> evaluations)
    {
        if (pullRequest.Status is not "active")
        {
            return null;
        }

        var blockingStatuses = evaluations.Where(e => e.IsBlocking).Select(e => e.Status).ToList();

        if (pullRequest.IsDraft
            || pullRequest.MergeStatus is "conflicts" or "failure"
            || blockingStatuses.Any(s => s is "rejected" or "broken"))
        {
            return "blocked";
        }

        if (pullRequest.MergeStatus is "queued" || blockingStatuses.Any(s => s is "queued" or "running"))
        {
            return "waiting";
        }

        return "ready";
    }

    private static PullRequestCheckResult BuildCheck(PolicyEvaluation evaluation)
    {
        return new PullRequestCheckResult
        {
            Name = evaluation.Name,
            IsBlocking = evaluation.IsBlocking,
            Status = evaluation.Status,
            StartedDate = evaluation.StartedDate is null ? null : FormatIsoUtc(evaluation.StartedDate.Value),
            CompletedDate = evaluation.CompletedDate is null ? null : FormatIsoUtc(evaluation.CompletedDate.Value),
            Context = evaluation.Context,
        };
    }

    private static string FormatIsoUtc(DateTimeOffset value)
    {
        return value.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);
    }
}
