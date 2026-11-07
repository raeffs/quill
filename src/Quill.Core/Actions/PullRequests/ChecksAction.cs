using Quill.Core.Models;

namespace Quill.Core.Actions.PullRequests;

public static class ChecksAction
{
    public static async Task<PullRequestChecksResult> RunAsync(
        IAzureDevOpsPullRequestClient pullRequests,
        int id,
        CancellationToken cancellationToken)
    {
        var pullRequest = await pullRequests.GetByIdAsync(id, cancellationToken);
        if (string.IsNullOrEmpty(pullRequest.ProjectId))
        {
            throw new InvalidOperationException($"Pull request {id} has no project.");
        }

        var evaluations = await pullRequests.GetPolicyEvaluationsAsync(
            pullRequest.ProjectId, pullRequest.CodeReviewId, cancellationToken);

        return PullRequestChecksResultBuilder.Build(pullRequest, evaluations);
    }
}
