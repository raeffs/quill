using Quill.Core.Models;

namespace Quill.Core.Actions.PullRequests;

public static class CreateAction
{
    public static async Task<PullRequestViewResult> RunAsync(
        IAzureDevOpsPullRequestClient pullRequests,
        PullRequestCreateRequest request,
        CancellationToken cancellationToken)
    {
        var created = await pullRequests.CreateAsync(request, cancellationToken);

        // The author is not a reviewer and a draft gains no policy-required reviewer, so myVote
        // and myIsRequired are null whatever the current user is. That saves the identity call.
        return PullRequestViewResultBuilder.Build(
            created,
            currentUserId: string.Empty,
            markdownDescription: created.Description,
            workItems: null,
            threads: null);
    }
}
