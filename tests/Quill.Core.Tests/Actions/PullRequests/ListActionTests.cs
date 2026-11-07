using Quill.Core.Actions.PullRequests;
using Quill.Core.Models;
using Quill.Tests.Shared.Fakes;
using Shouldly;

namespace Quill.Core.Tests.Actions.PullRequests;

public class ListActionTests
{
    [Fact]
    public async Task RunAsync_AuthorIsMe_ResolvesTheUserAndFiltersByCreator()
    {
        PullRequestListQuery? seen = null;
        var prs = new ScriptedPullRequestClient
        {
            List = q =>
            {
                seen = q;
                return [];
            },
        };
        var identity = new ScriptedIdentityClient();

        await ListAction.RunAsync(prs, identity, new PrListRequest(false, true, "active", null, null, null, 20, null), TestContext.Current.CancellationToken);

        seen!.CreatorId.ShouldBe("user-1");
        seen.ReviewerId.ShouldBeNull();
        identity.Calls.ShouldBe(["GetCurrentUser"]);
    }

    [Fact]
    public async Task RunAsync_ReviewerIsMe_FiltersByReviewer()
    {
        PullRequestListQuery? seen = null;
        var prs = new ScriptedPullRequestClient
        {
            List = q =>
            {
                seen = q;
                return [];
            },
        };

        await ListAction.RunAsync(prs, new ScriptedIdentityClient(), new PrListRequest(true, false, "all", "repo", "feature", "main", 5, 10), TestContext.Current.CancellationToken);

        seen!.ReviewerId.ShouldBe("user-1");
        seen.CreatorId.ShouldBeNull();
        seen.Status.ShouldBe("all");
        seen.Repo.ShouldBe("repo");
        seen.SourceBranch.ShouldBe("feature");
        seen.TargetBranch.ShouldBe("main");
        seen.Top.ShouldBe(5);
        seen.Skip.ShouldBe(10);
    }

    [Fact]
    public async Task RunAsync_NoFilters_ReturnsOneRowPerPullRequestAndStillResolvesTheUser()
    {
        var prs = new ScriptedPullRequestClient { List = _ => [PullRequest(1), PullRequest(2)] };
        var identity = new ScriptedIdentityClient();

        var rows = await ListAction.RunAsync(prs, identity, new PrListRequest(false, false, "active", null, null, null, 50, null), TestContext.Current.CancellationToken);

        rows.Select(r => r.Id).ShouldBe([1, 2]);
        identity.Calls.ShouldBe(["GetCurrentUser"]);
        prs.Calls.ShouldBe(["List"]);
    }

    private static PullRequest PullRequest(int id) => new()
    {
        Id = id,
        Title = "Add feature",
        AuthorDisplayName = "Jane Doe",
        Status = "active",
        IsDraft = false,
        RepoName = "repo",
        SourceBranch = "refs/heads/feature",
        TargetBranch = "refs/heads/main",
        CreatedDate = new DateTimeOffset(2026, 1, 1, 9, 0, 0, TimeSpan.Zero),
        Reviewers = [],
        WebUrl = "https://dev.example.com/Contoso/_git/repo/pullrequest/12",
        ProjectId = "proj",
        CodeReviewId = 99,
    };
}
