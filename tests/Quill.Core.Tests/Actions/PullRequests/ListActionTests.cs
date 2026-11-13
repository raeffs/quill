using Quill.Core;
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

        await ListAction.RunAsync(prs, identity, new PrListRequest(null, "@me", "active", null, null, null, 20, null), TestContext.Current.CancellationToken);

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

        await ListAction.RunAsync(prs, new ScriptedIdentityClient(), new PrListRequest("@me", null, "all", "repo", "feature", "main", 5, 10), TestContext.Current.CancellationToken);

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

        var rows = await ListAction.RunAsync(prs, identity, new PrListRequest(null, null, "active", null, null, null, 50, null), TestContext.Current.CancellationToken);

        rows.Select(r => r.Id).ShouldBe([1, 2]);
        identity.Calls.ShouldBe(["GetCurrentUser"]);
        prs.Calls.ShouldBe(["List"]);
    }

    [Fact]
    public async Task RunAsync_NamedReviewer_FiltersByTheResolvedId()
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
        var identity = new ScriptedIdentityClient
        {
            Search = (_, _) => [new IdentityMatch("id-2", "John Roe", @"CONTOSO\jroe", false)],
        };

        await ListAction.RunAsync(prs, identity, new PrListRequest("John Roe", null, "active", null, null, null, 20, null), TestContext.Current.CancellationToken);

        seen!.ReviewerId.ShouldBe("id-2");
        identity.Calls.ShouldBe(["Search(General,John Roe)", "GetCurrentUser"]);
    }

    [Fact]
    public async Task RunAsync_ReviewerAndAuthorAreMe_CallsConnectionDataOnce()
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

        await ListAction.RunAsync(prs, identity, new PrListRequest("@me", "@me", "active", null, null, null, 20, null), TestContext.Current.CancellationToken);

        identity.Calls.ShouldBe(["GetCurrentUser"]);
        seen!.ReviewerId.ShouldBe("user-1");
        seen.CreatorId.ShouldBe("user-1");
    }

    [Fact]
    public async Task RunAsync_AmbiguousAuthor_ThrowsBeforeListing()
    {
        var prs = new ScriptedPullRequestClient();
        var identity = new ScriptedIdentityClient
        {
            Search = (_, _) =>
            [
                new IdentityMatch("id-1", "Jane Doe", @"CONTOSO\jdoe", false),
                new IdentityMatch("id-2", "Jane Doe", @"CONTOSO\jdoe2", false),
            ],
        };

        await Should.ThrowAsync<RejectedException>(() =>
            ListAction.RunAsync(prs, identity, new PrListRequest(null, "Jane Doe", "active", null, null, null, 20, null), TestContext.Current.CancellationToken));

        prs.Calls.ShouldBeEmpty();
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
