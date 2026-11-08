using Quill.Core.Models;

namespace Quill.Core.Actions.PullRequests;

public sealed record PrListRequest(
    string? Reviewer,
    string? Author,
    string State,
    string? Repo,
    string? SourceBranch,
    string? TargetBranch,
    int Limit,
    int? Skip);

public static class ListAction
{
    public static async Task<IReadOnlyList<PullRequestResult>> RunAsync(
        IAzureDevOpsPullRequestClient pullRequests,
        IIdentityClient identity,
        PrListRequest request,
        CancellationToken cancellationToken)
    {
        var resolver = new PersonResolver(identity);
        var reviewer = request.Reviewer is null
            ? null
            : await resolver.ResolveAsync("--reviewer", request.Reviewer, cancellationToken);
        var author = request.Author is null
            ? null
            : await resolver.ResolveAsync("--author", request.Author, cancellationToken);
        var currentUser = await resolver.MeAsync();

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
