using Microsoft.Extensions.Logging.Abstractions;
using Quill.Core.Actions.PullRequests;
using Quill.Core.Markdown;
using Quill.Core.Models;
using Quill.Tests.Shared.Fakes;
using Shouldly;

namespace Quill.Core.Tests.Actions.PullRequests;

public class ViewActionTests
{
    private static readonly QuillConfig Config = new()
    {
        ServerUrl = "https://devops.example.com",
        Collection = "COL",
        Project = "PROJ",
        AllowedStates = ["New"],
    };

    [Fact]
    public async Task RunAsync_WithoutThreads_SendsNoThreadsRequest()
    {
        var (prs, workItems, identity) = Clients(workItemRefs: []);

        var row = await ViewAction.RunAsync(prs, workItems,  identity, new RichText(Config, workItems, NullLogger<RichText>.Instance), 12, false, TestContext.Current.CancellationToken);

        row.Threads.ShouldBeNull();
        prs.Calls.ShouldBe(["GetById(12)", "GetWorkItemRefs(12,repo)"]);
    }

    [Fact]
    public async Task RunAsync_WithThreads_SendsTheThreadsRequest()
    {
        var (prs, workItems, identity) = Clients(workItemRefs: []);
        prs.GetThreads = (_, _) => [];

        var row = await ViewAction.RunAsync(prs, workItems,  identity, new RichText(Config, workItems, NullLogger<RichText>.Instance), 12, true, TestContext.Current.CancellationToken);

        row.Threads.ShouldNotBeNull();
        prs.Calls.ShouldBe(["GetById(12)", "GetWorkItemRefs(12,repo)", "GetThreads(12,repo)"]);
    }

    [Fact]
    public async Task RunAsync_LinkedWorkItems_EmitsStubsForFailedAndUnreadableInRefOrder()
    {
        var (prs, workItems, identity) = Clients(workItemRefs: [201, 202, 203]);
        workItems.Add(WorkItem(201));
        workItems.BatchFailedIds.Add(202);

        var row = await ViewAction.RunAsync(prs, workItems,  identity, new RichText(Config, workItems, NullLogger<RichText>.Instance), 12, false, TestContext.Current.CancellationToken);

        row.WorkItems.ShouldNotBeNull();
        row.WorkItems.Select(w => w.Id).ShouldBe([201, 202, 203]);
        row.WorkItems[0].Error.ShouldBeNull();
        row.WorkItems[1].Error.ShouldBe("batch-failed");
        row.WorkItems[2].Error.ShouldBe("unreadable");
    }

    private static (ScriptedPullRequestClient Prs, FakeAzureDevOpsClient WorkItems, ScriptedIdentityClient Identity) Clients(int[] workItemRefs) =>
        (
            new ScriptedPullRequestClient
            {
                GetById = id => PullRequest(id),
                GetWorkItemRefs = (_, _) => workItemRefs,
            },
            new FakeAzureDevOpsClient(),
            new ScriptedIdentityClient());

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

    private static WorkItem WorkItem(int id) => new()
    {
        Id = id,
        Type = "Task",
        Title = "Write the importer",
        State = "New",
        AssignedToId = string.Empty,
    };
}
