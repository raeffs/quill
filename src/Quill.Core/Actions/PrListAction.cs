using Quill.Core.Models;

namespace Quill.Core.Actions;

public sealed record PrListRequest(
    bool ReviewerIsMe,
    bool AuthorIsMe,
    string State,
    string? Repo,
    string? SourceBranch,
    string? TargetBranch,
    int Limit,
    int? Skip);

public static class PrListAction
{
    public static async Task<IReadOnlyList<PullRequestResult>> RunAsync(
        IAzureDevOpsPullRequestClient pullRequests,
        IIdentityClient identity,
        PrListRequest request,
        CancellationToken cancellationToken)
    {
        var currentUser = await identity.GetCurrentUserAsync();
        var userId = currentUser.Id;

        var query = new PullRequestListQuery
        {
            CreatorId = request.AuthorIsMe ? userId : null,
            ReviewerId = request.ReviewerIsMe ? userId : null,
            Status = request.State,
            Repo = request.Repo,
            SourceBranch = request.SourceBranch,
            TargetBranch = request.TargetBranch,
            Top = request.Limit,
            Skip = request.Skip,
        };

        var results = await pullRequests.ListAsync(query, cancellationToken);

        return results
            .Select(pr => PullRequestResultBuilder.Build(pr, userId))
            .ToList();
    }
}
