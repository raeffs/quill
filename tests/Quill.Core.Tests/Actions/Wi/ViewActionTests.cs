using Microsoft.Extensions.Logging.Abstractions;
using Quill.Core.Actions.Wi;
using Quill.Core.Models;
using Shouldly;

namespace Quill.Core.Tests.Actions.Wi;

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
    public async Task RunAsync_WithoutChildren_SendsNoBatchFetch()
    {
        var client = new FakeAzureDevOpsClient();
        client.Add(WorkItem(5, [6, 7]));

        var row = await ViewAction.RunAsync(client, client, Config, NullLogger.Instance, 5, false, TestContext.Current.CancellationToken);

        row.Children.ShouldBeNull();
        client.Calls.ShouldBe(["GetWorkItem(5)"]);
    }

    [Fact]
    public async Task RunAsync_WithChildren_FetchesTheChildren()
    {
        var client = new FakeAzureDevOpsClient();
        client.Add(WorkItem(5, [6, 7]));
        client.Add(WorkItem(6, []));
        client.Add(WorkItem(7, []));

        var row = await ViewAction.RunAsync(client, client, Config, NullLogger.Instance, 5, true, TestContext.Current.CancellationToken);

        row.Children.ShouldNotBeNull();
        row.Children.Count.ShouldBe(2);
        client.Calls.Last().ShouldBe("Fetch(6,7)");
    }

    private static WorkItem WorkItem(int id, int[] childIds) => new()
    {
        Id = id,
        Type = "Task",
        Title = "Write the importer",
        State = "New",
        AssignedToId = string.Empty,
        ChildIds = childIds,
    };
}
