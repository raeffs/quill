using Microsoft.Extensions.Logging.Abstractions;
using Quill.Core.Actions.Wi;
using Quill.Core.Models;
using Quill.Tests.Shared;
using Shouldly;

namespace Quill.Core.Tests.Actions.Wi;

public class CreateActionTests
{
    private const int ParentId = 10;
    private const string ParentIterationPath = @"Proj\Sprint 7";
    private const string ParentAreaPath = @"Proj\Team A";

    private static readonly QuillConfig Config = new()
    {
        ServerUrl = TestConstants.ServerUrl,
        Collection = TestConstants.Collection,
        Project = TestConstants.Project,
        AllowedStates = ["Active", "New"],
    };

    [Fact]
    public async Task RunAsync_ParentOfWrongType_ThrowsNamingParentTypeAndCreatesNothing()
    {
        var client = Seed();

        var ex = await Should.ThrowAsync<InvalidRequestException>(() => CreateAction.RunAsync(client, Config, NullLogger.Instance, Request("Product Backlog Item")));

        ex.Message.ShouldContain("'Product Backlog Item'");
        client.CreateCall.ShouldBeNull();
    }

    [Theory]
    [InlineData("New")]
    [InlineData("Closed")]
    public async Task RunAsync_ParentAssignedToSomeoneElseInAnyState_Creates(string parentState)
    {
        var client = Seed(parentState);

        var row = await CreateAction.RunAsync(client, Config, NullLogger.Instance, Request("Task"));

        row.Id.ShouldBe(1000);
    }

    [Fact]
    public async Task RunAsync_NoAssignee_CreatesUnassigned()
    {
        var client = Seed();

        var row = await CreateAction.RunAsync(client, Config, NullLogger.Instance, Request("Task"));

        client.CreateCall.ShouldNotBeNull().AssignedToId.ShouldBeNull();
        row.AssignedTo.ShouldBeNull();
    }

    [Fact]
    public async Task RunAsync_WithAssignee_AssignsThatUser()
    {
        var client = Seed();

        var row = await CreateAction.RunAsync(client, Config, NullLogger.Instance, Request("Task", assignee: "Jane Doe"));

        client.CreateCall.ShouldNotBeNull().AssignedToId.ShouldBe("Jane Doe");
        row.AssignedTo.ShouldBe("Jane Doe");
    }

    [Fact]
    public async Task RunAsync_UsesParentIterationPath()
    {
        var client = Seed();

        await CreateAction.RunAsync(client, Config, NullLogger.Instance, Request("Task"));

        client.CreateCall.ShouldNotBeNull().IterationPath.ShouldBe(ParentIterationPath);
    }

    [Theory]
    [InlineData("Feature", "Epic")]
    [InlineData("Product Backlog Item", "Feature")]
    [InlineData("Bug", "Feature")]
    [InlineData("Task", "Product Backlog Item")]
    [InlineData("Task", "Bug")]
    public async Task RunAsync_UsesParentAreaPath(string type, string parentType)
    {
        var client = Seed(parentType: parentType);

        await CreateAction.RunAsync(client, Config, NullLogger.Instance, Request(type));

        client.CreateCall.ShouldNotBeNull().AreaPath.ShouldBe(ParentAreaPath);
    }

    [Fact]
    public async Task RunAsync_DescriptionLink_BecomesRelatedRelation()
    {
        var client = Seed();

        var row = await CreateAction.RunAsync(
            client, Config, NullLogger.Instance, Request("Task", description: "See [the bug](#123)."));

        row.RelatedIds.ShouldBe([123]);
        client.CreateCall.ShouldNotBeNull().DescriptionHtml.ShouldNotBeNull().ShouldContain("_workitems/edit/123");
    }

    [Fact]
    public async Task RunAsync_NoDescription_SendsNoDescription()
    {
        var client = Seed();

        var row = await CreateAction.RunAsync(client, Config, NullLogger.Instance, Request("Task"));

        client.CreateCall.ShouldNotBeNull().DescriptionHtml.ShouldBeNull();
        row.Description.ShouldBe(string.Empty);
    }

    [Fact]
    public async Task RunAsync_ReturnsTheViewRowOfTheCreatedItem()
    {
        var client = Seed();

        var row = await CreateAction.RunAsync(client, Config, NullLogger.Instance, Request("Task"));

        var created = row.ShouldNotBeNull();
        created.Id.ShouldBe(1000);
        created.Type.ShouldBe("Task");
        created.Title.ShouldBe("Write the importer");
        created.State.ShouldBe("New");
        created.ParentId.ShouldBe(ParentId);
        created.Children.ShouldBeNull();
    }

    [Fact]
    public async Task RunAsync_FeatureUnderEpic_CreatesUnassignedWithEpicIterationPath()
    {
        var client = Seed(parentType: "Epic");

        var row = await CreateAction.RunAsync(client, Config, NullLogger.Instance, Request("Feature"));

        row.Type.ShouldBe("Feature");
        var call = client.CreateCall.ShouldNotBeNull();
        call.AssignedToId.ShouldBeNull();
        call.IterationPath.ShouldBe(ParentIterationPath);
    }

    [Fact]
    public async Task RunAsync_FeatureUnderNonEpic_ThrowsAndCreatesNothing()
    {
        var client = Seed();

        var ex = await Should.ThrowAsync<InvalidRequestException>(() => CreateAction.RunAsync(client, Config, NullLogger.Instance, Request("Feature")));

        ex.Message.ShouldContain("'Product Backlog Item'");
        client.CreateCall.ShouldBeNull();
    }

    [Fact]
    public async Task RunAsync_WithTags_SendsTags()
    {
        var client = Seed();

        await CreateAction.RunAsync(client, Config, NullLogger.Instance, Request("Task", tags: ["a", "b"]));

        client.CreateCall.ShouldNotBeNull().Tags.ShouldBe(["a", "b"]);
    }

    [Fact]
    public async Task RunAsync_WithTagsAndDescriptionLink_RowCarriesTags()
    {
        var client = Seed();

        var row = await CreateAction.RunAsync(
            client, Config, NullLogger.Instance, Request("Task", description: "See [the bug](#123).", tags: ["a", "b"]));

        var created = row.ShouldNotBeNull();
        created.Tags.ShouldBe(["a", "b"]);
        created.RelatedIds.ShouldBe([123]);
    }

    [Fact]
    public async Task RunAsync_NoTags_RowHasEmptyTags()
    {
        var client = Seed();

        var row = await CreateAction.RunAsync(client, Config, NullLogger.Instance, Request("Task"));

        row.Tags.ShouldBeEmpty();
    }

    [Fact]
    public async Task RunAsync_WithPredecessors_SendsLinksInTheCreatePatch()
    {
        var client = Seed();

        await CreateAction.RunAsync(client, Config, NullLogger.Instance, Request("Task", predecessors: [10, 11]));

        client.CreateCall.ShouldNotBeNull().Predecessors.ShouldBe([10, 11]);
    }

    [Fact]
    public async Task RunAsync_WithPredecessorsAndDescriptionLink_RowCarriesPredecessors()
    {
        var client = Seed();

        var row = await CreateAction.RunAsync(
            client, Config, NullLogger.Instance, Request("Task", description: "See [the bug](#123).", predecessors: [10, 11]));

        var created = row.ShouldNotBeNull();
        created.Predecessors.ShouldBe([10, 11]);
        created.RelatedIds.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task RunAsync_NoPredecessors_RowHasEmptyPredecessors()
    {
        var client = Seed();

        var row = await CreateAction.RunAsync(client, Config, NullLogger.Instance, Request("Task"));

        row.Predecessors.ShouldBeEmpty();
    }

    private static WorkItemCreateRequest Request(
        string type,
        string? description = null,
        string? assignee = null,
        IReadOnlyList<string>? tags = null,
        IReadOnlyList<int>? predecessors = null) =>
        new(type, "Write the importer", ParentId, description, assignee, tags, predecessors);

    private static FakeAzureDevOpsClient Seed(string parentState = "Active", string parentType = "Product Backlog Item")
    {
        var client = new FakeAzureDevOpsClient();
        client.Add(new WorkItem
        {
            Id = ParentId,
            Type = parentType,
            Title = "The parent",
            State = parentState,
            AssignedToId = "other-user-id",
            IterationPath = ParentIterationPath,
            AreaPath = ParentAreaPath,
        });
        client.Add(new WorkItem
        {
            Id = 123,
            Type = "Bug",
            Title = "The bug",
            State = "Active",
            AssignedToId = string.Empty,
        });
        return client;
    }
}
