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
            Status = pullRequest.Status,
            MergeStatus = pullRequest.MergeStatus,
            IsDraft = pullRequest.IsDraft,
            Checks = evaluations.Select(BuildCheck).ToList(),
        };
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
