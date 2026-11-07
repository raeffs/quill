using Microsoft.Extensions.Logging.Abstractions;
using Quill.Core.Actions.Backlog;
using Quill.Core.Models;
using Quill.Tests.Shared;
using Quill.Tests.Shared.Fakes;
using Shouldly;

namespace Quill.Core.Tests.Actions.Backlog;

public class PushActionTests
{
    private static readonly QuillConfig Config = new()
    {
        ServerUrl = TestConstants.ServerUrl,
        Collection = TestConstants.Collection,
        Project = TestConstants.Project,
        AllowedStates = ["New"],
    };

    [Fact]
    public async Task RunAsync_FileWithoutTagsKey_SendsNoTagsAndOmitsTagsFromUpdatedFields()
    {
        var client = new FakeAzureDevOpsClient();
        client.Add(WorkItem(5, state: "New"));
        WorkItemUpdatePatch? seen = null;
        client.UpdateWorkItem = p => seen = p;

        var row = await PushAction.RunAsync(client, new ScriptedIdentityClient(), Config, NullLogger.Instance, "---\nid: 5\ntype: Task\ntitle: T\nparentId: 42\n---\n\nbody\n");

        seen!.Tags.ShouldBeNull();
        row.Id.ShouldBe(5);
        row.Title.ShouldBe("T");
        row.UpdatedFields.ShouldBe(["title", "description"]);
        row.RelationsAdded.ShouldBeEmpty();
    }

    [Fact]
    public async Task RunAsync_FileWithTagsKey_SendsTagsAndListsThemInUpdatedFields()
    {
        var client = new FakeAzureDevOpsClient();
        client.Add(WorkItem(5, state: "New"));
        WorkItemUpdatePatch? seen = null;
        client.UpdateWorkItem = p => seen = p;

        var row = await PushAction.RunAsync(client, new ScriptedIdentityClient(), Config, NullLogger.Instance, "---\nid: 5\ntype: Task\ntitle: T\nparentId: 42\ntags: [a, b]\n---\n\nbody\n");

        seen!.Tags.ShouldBe(["a", "b"]);
        row.UpdatedFields.ShouldBe(["title", "description", "tags"]);
    }

    [Fact]
    public async Task RunAsync_InvalidState_ThrowsAndSendsNoUpdate()
    {
        var client = new FakeAzureDevOpsClient();
        client.Add(WorkItem(5, state: "Done"));

        var ex = await Should.ThrowAsync<InvalidRequestException>(() => PushAction.RunAsync(client, new ScriptedIdentityClient(), Config, NullLogger.Instance, "---\nid: 5\ntype: Task\ntitle: T\n---\n\nbody\n"));

        ex.Message.ShouldBe("Work item state 'Done' is not in allowed states: [New]");
        client.Calls.Contains("UpdateWorkItem(5)", StringComparer.Ordinal).ShouldBeFalse();
    }

    [Fact]
    public async Task RunAsync_BodyLinksToExistingRelatedItem_AddsOnlyTheMissingRelation()
    {
        var client = new FakeAzureDevOpsClient();
        client.Add(WorkItem(5, state: "New", relations: [new WorkItemRelation { RelationType = WorkItemLinkTypes.Related, TargetId = 100 }]));

        var row = await PushAction.RunAsync(client, new ScriptedIdentityClient(), Config, NullLogger.Instance, "---\nid: 5\ntype: Task\ntitle: T\n---\n\n[a](#100) [b](#200)\n");

        client.Calls.TakeLast(2).ShouldBe(["UpdateWorkItem(5)", "AddRelation(5,200)"]);
        row.RelationsAdded.ShouldBe([200]);
    }

    private static WorkItem WorkItem(int id, string state, IReadOnlyList<WorkItemRelation>? relations = null) => new()
    {
        Id = id,
        Type = "Task",
        Title = "Old",
        State = state,
        AssignedToId = "user-1",
        Relations = relations ?? [],
    };
}
