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

        await ListAction.RunAsync(prs, new IdentityResolver(identity, TestPersonCache.Create()), new PrListRequest(null, "@me", "active", null, null, null, 20, null), TestContext.Current.CancellationToken);

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

        await ListAction.RunAsync(prs, new IdentityResolver(new ScriptedIdentityClient(), TestPersonCache.Create()), new PrListRequest("@me", null, "all", "repo", "feature", "main", 5, 10), TestContext.Current.CancellationToken);

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

        var rows = await ListAction.RunAsync(prs, new IdentityResolver(identity, TestPersonCache.Create()), new PrListRequest(null, null, "active", null, null, null, 50, null), TestContext.Current.CancellationToken);

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

        await ListAction.RunAsync(prs, new IdentityResolver(identity, TestPersonCache.Create()), new PrListRequest("John Roe", null, "active", null, null, null, 20, null), TestContext.Current.CancellationToken);

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

        await ListAction.RunAsync(prs, new IdentityResolver(identity, TestPersonCache.Create()), new PrListRequest("@me", "@me", "active", null, null, null, 20, null), TestContext.Current.CancellationToken);

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
            ListAction.RunAsync(prs, new IdentityResolver(identity, TestPersonCache.Create()), new PrListRequest(null, "Jane Doe", "active", null, null, null, 20, null), TestContext.Current.CancellationToken));

        prs.Calls.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("Contoso Platform")]
    [InlineData(@"[Fabrikam]\Contoso Platform")]
    public async Task RunAsync_GroupReviewer_FiltersByTheGroupId(string reviewer)
    {
        var prs = new ScriptedPullRequestClient();
        var identity = new ScriptedIdentityClient { Search = OnlyTheGroup };

        var seen = await Run(prs, identity, new InMemoryPersonStore(), reviewer: reviewer);

        seen.ReviewerId.ShouldBe("id-3");
        seen.CreatorId.ShouldBeNull();
        identity.Calls.ShouldBe([$"Search(General,{reviewer})", "GetCurrentUser"]);
    }

    [Fact]
    public async Task RunAsync_GroupReviewerWithNoPullRequests_ReturnsNoRows()
    {
        var prs = new ScriptedPullRequestClient { List = _ => [] };
        var identity = new ScriptedIdentityClient { Search = OnlyTheGroup };

        var rows = await ListAction.RunAsync(prs, new IdentityResolver(identity, TestPersonCache.Create()), new PrListRequest("Contoso Platform", null, "active", null, null, null, 20, null), TestContext.Current.CancellationToken);

        rows.ShouldBeEmpty();
    }

    [Fact]
    public async Task RunAsync_ReviewerMatchesAPersonAndAGroup_ThrowsBeforeListing()
    {
        var prs = new ScriptedPullRequestClient();
        var identity = new ScriptedIdentityClient { Search = PersonAndGroup };

        var ex = await Should.ThrowAsync<RejectedException>(() => Run(prs, identity, new InMemoryPersonStore(), reviewer: "Platform"));

        ex.Message.ShouldBe(@"--reviewer 'Platform' matches 1 person and 1 group: Platform (CONTOSO\svc-platform), [Fabrikam]\Platform. Pass the unique name or the scoped name.");
        prs.Calls.ShouldBeEmpty();
    }

    [Fact]
    public async Task RunAsync_AuthorMatchesAPersonAndAGroup_FiltersByThePerson()
    {
        var identity = new ScriptedIdentityClient { Search = PersonAndGroup };

        var seen = await Run(new ScriptedPullRequestClient(), identity, new InMemoryPersonStore(), author: "Platform");

        seen.CreatorId.ShouldBe("id-4");
    }

    [Fact]
    public async Task RunAsync_GroupAuthor_Throws()
    {
        var prs = new ScriptedPullRequestClient();
        var identity = new ScriptedIdentityClient { Search = OnlyTheGroup };

        var ex = await Should.ThrowAsync<RejectedException>(() => Run(prs, identity, new InMemoryPersonStore(), author: "Contoso Platform"));

        ex.Message.ShouldBe("--author 'Contoso Platform' matches only groups. Groups are not accepted; name a person.");
        prs.Calls.ShouldBeEmpty();
    }

    [Fact]
    public async Task RunAsync_SecondRunWithTheSameGroupReviewer_SendsNoSearch()
    {
        var store = new InMemoryPersonStore();
        await Run(new ScriptedPullRequestClient(), new ScriptedIdentityClient { Search = OnlyTheGroup }, store, reviewer: "Contoso Platform");
        var identity = new ScriptedIdentityClient();

        var seen = await Run(new ScriptedPullRequestClient(), identity, store, reviewer: "Contoso Platform");

        seen.ReviewerId.ShouldBe("id-3");
        identity.Calls.ShouldBeEmpty();
    }

    [Fact]
    public async Task RunAsync_SameNameAsAuthorThenAsReviewer_SearchesTwice()
    {
        var store = new InMemoryPersonStore();
        await Run(new ScriptedPullRequestClient(), new ScriptedIdentityClient { Search = PersonAndGroup }, store, author: "Platform");
        var prs = new ScriptedPullRequestClient();
        var identity = new ScriptedIdentityClient { Search = PersonAndGroup };

        await Should.ThrowAsync<RejectedException>(() => Run(prs, identity, store, reviewer: "Platform"));

        identity.Calls.ShouldBe(["Search(General,Platform)"]);
        prs.Calls.ShouldBeEmpty();
    }

    private static IReadOnlyList<IdentityMatch> OnlyTheGroup(IdentitySearchFilter filter, string value)
        => [new IdentityMatch("id-3", @"[Fabrikam]\Contoso Platform", null, true)];

    private static IReadOnlyList<IdentityMatch> PersonAndGroup(IdentitySearchFilter filter, string value)
        =>
        [
            new IdentityMatch("id-3", @"[Fabrikam]\Platform", null, true),
            new IdentityMatch("id-4", "Platform", @"CONTOSO\svc-platform", false),
        ];

    private static async Task<PullRequestListQuery> Run(
        ScriptedPullRequestClient prs,
        ScriptedIdentityClient identity,
        InMemoryPersonStore store,
        string? reviewer = null,
        string? author = null)
    {
        PullRequestListQuery? seen = null;
        prs.List = query =>
        {
            seen = query;
            return [];
        };

        await ListAction.RunAsync(prs, new IdentityResolver(identity, TestPersonCache.Create(store)), new PrListRequest(reviewer, author, "active", null, null, null, 20, null), TestContext.Current.CancellationToken);

        return seen.ShouldNotBeNull();
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
