using Quill.Core.Models;

namespace Quill.Core.Actions;

public static class PrRevisionsAction
{
    public static async Task<IReadOnlyList<PullRequestRevisionResult>> RunAsync(
        IAzureDevOpsPullRequestClient client,
        int id,
        CancellationToken cancellationToken)
    {
        var pullRequest = await client.GetByIdAsync(id, cancellationToken);
        var revisions = await client.GetRevisionsAsync(id, pullRequest.RepoName, cancellationToken);

        var results = new List<PullRequestRevisionResult>(revisions.Count);
        foreach (var revision in revisions)
        {
            results.Add(PullRequestRevisionResultBuilder.Build(revision));
        }

        return results;
    }
}
