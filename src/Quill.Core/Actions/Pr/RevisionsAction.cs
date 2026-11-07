using Quill.Core.Models;

namespace Quill.Core.Actions.Pr;

public static class RevisionsAction
{
    public static async Task<IReadOnlyList<PullRequestRevisionResult>> RunAsync(
        IAzureDevOpsPullRequestClient pullRequests,
        int id,
        CancellationToken cancellationToken)
    {
        var pullRequest = await pullRequests.GetByIdAsync(id, cancellationToken);
        var revisions = await pullRequests.GetRevisionsAsync(id, pullRequest.RepoName, cancellationToken);

        var results = new List<PullRequestRevisionResult>(revisions.Count);
        foreach (var revision in revisions)
        {
            results.Add(PullRequestRevisionResultBuilder.Build(revision));
        }

        return results;
    }
}
