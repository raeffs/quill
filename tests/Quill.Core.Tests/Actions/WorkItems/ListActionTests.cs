using Quill.Core.Actions.WorkItems;
using Quill.Core.Models;
using Quill.Tests.Shared.Fakes;
using Shouldly;

namespace Quill.Core.Tests.Actions.WorkItems;

public class ListActionTests
{
    [Fact]
    public async Task RunAsync_QueriesThenFetchesAndKeepsQueryOrder()
    {
        var client = new FakeAzureDevOpsClient { QueryByWiql = (_, _) => [3, 1, 2] };
        client.Add(WorkItem(1));
        client.Add(WorkItem(2));
        client.Add(WorkItem(3));

        var rows = await ListAction.RunAsync(client, client, new ScriptedIdentityClient(), new WiListRequest("importer", null, [], [], 20), TestContext.Current.CancellationToken);

        rows.Select(r => r.Id).ShouldBe([3, 1, 2]);
        client.Calls.ShouldBe(["QueryByWiql", "Fetch(3,1,2)"]);
    }

    [Fact]
    public async Task RunAsync_SkipsIdsTheBatchDidNotReturn()
    {
        var client = new FakeAzureDevOpsClient { QueryByWiql = (_, _) => [3, 1, 2] };
        client.Add(WorkItem(1));
        client.Add(WorkItem(3));

        var rows = await ListAction.RunAsync(client, client, new ScriptedIdentityClient(), new WiListRequest("importer", null, [], [], 20), TestContext.Current.CancellationToken);

        rows.Select(r => r.Id).ShouldBe([3, 1]);
    }

    [Fact]
    public async Task RunAsync_NamedAssignee_QueriesTheCombinedForm()
    {
        string? wiql = null;
        var client = new FakeAzureDevOpsClient
        {
            QueryByWiql = (w, _) =>
            {
                wiql = w;
                return [];
            },
        };
        var identity = new ScriptedIdentityClient
        {
            Search = (_, _) => [new IdentityMatch("id-1", "Jane Doe", @"CONTOSO\jdoe", false)],
        };

        await ListAction.RunAsync(client, client, identity, new WiListRequest(null, "Jane Doe", [], [], 20), TestContext.Current.CancellationToken);

        wiql.ShouldNotBeNull().ShouldContain(@"[System.AssignedTo] = 'Jane Doe <CONTOSO\jdoe>'");
    }

    [Fact]
    public async Task RunAsync_AssigneeMe_UsesTheMacroAndNoIdentityRequest()
    {
        string? wiql = null;
        var client = new FakeAzureDevOpsClient
        {
            QueryByWiql = (w, _) =>
            {
                wiql = w;
                return [];
            },
        };
        var identity = new ScriptedIdentityClient();

        await ListAction.RunAsync(client, client, identity, new WiListRequest(null, "@me", [], [], 20), TestContext.Current.CancellationToken);

        wiql.ShouldNotBeNull().ShouldContain("[System.AssignedTo] = @Me");
        identity.Calls.ShouldBeEmpty();
    }

    [Fact]
    public async Task RunAsync_AssigneeWithApostrophe_DoublesTheQuote()
    {
        string? wiql = null;
        var client = new FakeAzureDevOpsClient
        {
            QueryByWiql = (w, _) =>
            {
                wiql = w;
                return [];
            },
        };
        var identity = new ScriptedIdentityClient
        {
            Search = (_, _) => [new IdentityMatch("id", "Pat O'Brien", @"CONTOSO\pobrien", false)],
        };

        await ListAction.RunAsync(client, client, identity, new WiListRequest(null, "Pat O'Brien", [], [], 20), TestContext.Current.CancellationToken);

        wiql.ShouldNotBeNull().ShouldContain(@"'Pat O''Brien <CONTOSO\pobrien>'");
    }

    [Fact]
    public async Task RunAsync_UnknownAssignee_ThrowsBeforeTheQuery()
    {
        var client = new FakeAzureDevOpsClient();

        var exception = await Should.ThrowAsync<InvalidRequestException>(() =>
            ListAction.RunAsync(client, client, new ScriptedIdentityClient(), new WiListRequest(null, "Jane", [], [], 20), TestContext.Current.CancellationToken));

        exception.Message.ShouldBe("--assignee 'Jane' matches nobody.");
        client.Calls.ShouldBeEmpty();
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
