using Quill.Core.Actions.PullRequests;
using Quill.Core.Models;
using Quill.Tests.Shared.Fakes;
using Shouldly;

namespace Quill.Core.Tests.Actions.PullRequests;

public class CreateActionTests
{
    [Fact]
    public async Task RunAsync_PassesTheRequestToCreateAndReturnsTheCreatedRow()
    {
        PullRequestCreateRequest? seen = null;
        var prs = new ScriptedPullRequestClient
        {
            Create = r =>
            {
                seen = r;
                return Created(77);
            },
        };
        var request = new PullRequestCreateRequest
        {
            Repo = "repo",
            SourceBranch = "feature",
            Title = "Add feature",
            Description = "Details",
            WorkItemIds = [4711],
        };

        var row = await CreateAction.RunAsync(prs, request, TestContext.Current.CancellationToken);

        seen.ShouldBeSameAs(request);
        row.Id.ShouldBe(77);
        row.MyVote.ShouldBeNull();
        prs.Calls.ShouldBe(["Create"]);
    }

    private static PullRequest Created(int id) => new()
    {
        Id = id,
        Title = "Add feature",
        AuthorDisplayName = "Jane Doe",
        Status = "active",
        IsDraft = true,
        RepoName = "repo",
        SourceBranch = "refs/heads/feature",
        TargetBranch = "refs/heads/main",
        CreatedDate = new DateTimeOffset(2026, 1, 1, 9, 0, 0, TimeSpan.Zero),
        Reviewers = [],
        WebUrl = "https://dev.example.com/Contoso/_git/repo/pullrequest/77",
        ProjectId = "proj",
        CodeReviewId = 99,
        Description = "Details",
    };
}
