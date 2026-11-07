using Microsoft.Extensions.Logging.Abstractions;
using Quill.Core.Actions.PullRequests;
using Quill.Core.Markdown;
using Quill.Core.Models;
using Quill.Tests.Shared.Fakes;
using Shouldly;

namespace Quill.Core.Tests.Actions.PullRequests;

public class ThreadsActionTests
{
    private static readonly QuillConfig Config = new()
    {
        ServerUrl = "https://devops.example.com",
        Collection = "COL",
        Project = "PROJ",
        AllowedStates = ["New"],
    };

    [Fact]
    public async Task RunAsync_FiltersByStatusThenLimits()
    {
        var client = new ScriptedPullRequestClient
        {
            GetById = id => PullRequest(id),
            GetThreads = (_, _) => [Thread(1, "active"), Thread(2, "closed"), Thread(3, "active"), Thread(4, "active")],
        };

        var rows = await ThreadsAction.RunAsync(client, new RichText(Config, new FakeAzureDevOpsClient(), NullLogger<RichText>.Instance), new PrThreadsRequest(12, ["active"], 2), TestContext.Current.CancellationToken);

        rows.Select(r => r.Id).ShouldBe([1, 3]);
        client.Calls.ShouldBe(["GetById(12)", "GetThreads(12,repo)"]);
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
    };

    private static PullRequestThread Thread(int id, string status) => new()
    {
        Id = id,
        Status = status,
        PublishedDate = new DateTimeOffset(2026, 1, 1, 9, 0, 0, TimeSpan.Zero),
        LastUpdatedDate = new DateTimeOffset(2026, 1, 1, 9, 0, 0, TimeSpan.Zero),
        Comments = [],
    };
}
