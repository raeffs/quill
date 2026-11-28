using Quill.Core.Models;

namespace Quill.Core.Actions.PullRequests;

public static class ListAction
{
    public static async Task<IReadOnlyList<PullRequestResult>> RunAsync(
        IAzureDevOpsPullRequestClient pullRequests,
        IdentityResolver people,
        PrListRequest request,
        CancellationToken cancellationToken)
    {
        var reviewer = request.Reviewer is null
            ? null
            : await people.ResolveAsync("--reviewer", request.Reviewer, true, cancellationToken);
        var author = request.Author is null
            ? null
            : await people.ResolveAsync("--author", request.Author, false, cancellationToken);
        var currentUser = await people.MeAsync();

        var query = new PullRequestListQuery
        {
            CreatorId = author?.Id,
            ReviewerId = reviewer?.Id,
            Status = request.State,
            Repo = request.Repo,
            SourceBranch = request.SourceBranch,
            TargetBranch = request.TargetBranch,
            Top = request.Limit,
            Skip = request.Skip,
        };

        var results = await pullRequests.ListAsync(query, cancellationToken);

        return results
            .Select(pr => PullRequestResultBuilder.Build(pr, currentUser.Id))
            .ToList();
    }
}
