using Quill.Core.Actions;
using Quill.Core.Models;
using Shouldly;

namespace Quill.Core.Tests.Actions;

public class WiTreeActionTests
{
    [Fact]
    public async Task RunAsync_ReadsTheRootAndPassesTheDepthThrough()
    {
        var client = new FakeAzureDevOpsClient();
        client.Add(WorkItem(5, [6]));
        client.Add(WorkItem(6, [7]));
        client.Add(WorkItem(7, []));

        var tree = await WiTreeAction.RunAsync(client, client, 5, 1, TestContext.Current.CancellationToken);

        tree.Id.ShouldBe(5);
        client.Calls.ShouldBe(["GetWorkItem(5)", "Fetch(6)"]);
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
