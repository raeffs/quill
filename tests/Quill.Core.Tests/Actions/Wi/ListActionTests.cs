using Quill.Core.Actions.Wi;
using Quill.Core.Models;
using Shouldly;

namespace Quill.Core.Tests.Actions.Wi;

public class ListActionTests
{
    [Fact]
    public async Task RunAsync_QueriesThenFetchesAndKeepsQueryOrder()
    {
        var client = new FakeAzureDevOpsClient { QueryByWiql = (_, _) => [3, 1, 2] };
        client.Add(WorkItem(1));
        client.Add(WorkItem(2));
        client.Add(WorkItem(3));

        var rows = await ListAction.RunAsync(client, client, new WiListRequest("importer", null, [], [], 20), TestContext.Current.CancellationToken);

        rows.Select(r => r.Id).ShouldBe([3, 1, 2]);
        client.Calls.ShouldBe(["QueryByWiql", "Fetch(3,1,2)"]);
    }

    [Fact]
    public async Task RunAsync_SkipsIdsTheBatchDidNotReturn()
    {
        var client = new FakeAzureDevOpsClient { QueryByWiql = (_, _) => [3, 1, 2] };
        client.Add(WorkItem(1));
        client.Add(WorkItem(3));

        var rows = await ListAction.RunAsync(client, client, new WiListRequest("importer", null, [], [], 20), TestContext.Current.CancellationToken);

        rows.Select(r => r.Id).ShouldBe([3, 1]);
    }

    private static WorkItem WorkItem(int id) => new()
    {
        Id = id,
        Type = "Task",
        Title = "Write the importer",
        State = "New",
        AssignedToId = string.Empty,
    };
}
