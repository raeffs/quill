using Quill.Core.Models;

namespace Quill.Core.Actions;

public static class PrChecksAction
{
    public static async Task<PullRequestChecksResult> RunAsync(
        IAzureDevOpsPullRequestClient client,
        int id,
        CancellationToken cancellationToken)
    {
        var pullRequest = await client.GetByIdAsync(id, cancellationToken);
        if (string.IsNullOrEmpty(pullRequest.ProjectId))
        {
            throw new InvalidOperationException($"Pull request {id} has no project.");
        }

        var evaluations = await client.GetPolicyEvaluationsAsync(
            pullRequest.ProjectId, pullRequest.CodeReviewId, cancellationToken);

        return PullRequestChecksResultBuilder.Build(pullRequest, evaluations);
    }
}
