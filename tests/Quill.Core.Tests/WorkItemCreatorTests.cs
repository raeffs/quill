using Quill.Core.Models;
using Quill.Tests.Shared;
using Shouldly;

namespace Quill.Core.Tests;

public class WorkItemCreatorTests
{
    private const int ParentId = 10;
    private const string ParentIterationPath = @"Proj\Sprint 7";

    private static readonly QuillConfig Config = new()
    {
        ServerUrl = TestConstants.ServerUrl,
        Collection = TestConstants.Collection,
        Project = TestConstants.Project,
        AllowedStates = ["Active", "New"],
    };

    [Fact]
    public async Task CreateAsync_ParentOfWrongType_ReturnsErrorNamingParentTypeAndCreatesNothing()
    {
        var client = Seed();

        var outcome = await WorkItemCreator.CreateAsync(client, Config, Request("Product Backlog Item"));

        outcome.Validation.IsValid.ShouldBeFalse();
        outcome.Validation.Errors.ShouldHaveSingleItem().ShouldContain("'Product Backlog Item'");
        outcome.Created.ShouldBeNull();
        client.CreateCall.ShouldBeNull();
    }

    [Theory]
    [InlineData("New")]
    [InlineData("Closed")]
    public async Task CreateAsync_ParentAssignedToSomeoneElseInAnyState_Creates(string parentState)
    {
        var client = Seed(parentState);

        var outcome = await WorkItemCreator.CreateAsync(client, Config, Request("Task"));

        outcome.Validation.IsValid.ShouldBeTrue();
        outcome.Created.ShouldNotBeNull();
    }

    [Fact]
    public async Task CreateAsync_NoAssignee_CreatesUnassigned()
    {
        var client = Seed();

        var outcome = await WorkItemCreator.CreateAsync(client, Config, Request("Task"));

        client.CreateCall.ShouldNotBeNull().AssignedToId.ShouldBeNull();
        outcome.Created.ShouldNotBeNull().AssignedTo.ShouldBeNull();
    }

    [Fact]
    public async Task CreateAsync_WithAssignee_AssignsThatUser()
    {
        var client = Seed();

        var outcome = await WorkItemCreator.CreateAsync(client, Config, Request("Task", assignee: "Jane Doe"));

        client.CreateCall.ShouldNotBeNull().AssignedToId.ShouldBe("Jane Doe");
        outcome.Created.ShouldNotBeNull().AssignedTo.ShouldBe("Jane Doe");
    }

    [Fact]
    public async Task CreateAsync_UsesParentIterationPath()
    {
        var client = Seed();

        await WorkItemCreator.CreateAsync(client, Config, Request("Task"));

        client.CreateCall.ShouldNotBeNull().IterationPath.ShouldBe(ParentIterationPath);
    }

    [Fact]
    public async Task CreateAsync_DescriptionLink_BecomesRelatedRelation()
    {
        var client = Seed();

        var outcome = await WorkItemCreator.CreateAsync(
            client, Config, Request("Task", description: "See [the bug](#123)."));

        outcome.Created.ShouldNotBeNull().RelatedIds.ShouldBe([123]);
        client.CreateCall.ShouldNotBeNull().DescriptionHtml.ShouldNotBeNull().ShouldContain("_workitems/edit/123");
    }

    [Fact]
    public async Task CreateAsync_NoDescription_SendsNoDescription()
    {
        var client = Seed();

        var outcome = await WorkItemCreator.CreateAsync(client, Config, Request("Task"));

        client.CreateCall.ShouldNotBeNull().DescriptionHtml.ShouldBeNull();
        outcome.Created.ShouldNotBeNull().Description.ShouldBe(string.Empty);
    }

    [Fact]
    public async Task CreateAsync_ReturnsTheViewRowOfTheCreatedItem()
    {
        var client = Seed();

        var outcome = await WorkItemCreator.CreateAsync(client, Config, Request("Task"));

        var created = outcome.Created.ShouldNotBeNull();
        created.Id.ShouldBe(1000);
        created.Type.ShouldBe("Task");
        created.Title.ShouldBe("Write the importer");
        created.State.ShouldBe("New");
        created.ParentId.ShouldBe(ParentId);
        created.Children.ShouldBeNull();
    }

    private static WorkItemCreateRequest Request(string type, string? description = null, string? assignee = null) =>
        new(type, "Write the importer", ParentId, description, assignee);

    private static FakeAzureDevOpsClient Seed(string parentState = "Active")
    {
        var client = new FakeAzureDevOpsClient();
        client.Add(new WorkItem
        {
            Id = ParentId,
            Type = "Product Backlog Item",
            Title = "The parent",
            State = parentState,
            AssignedToId = "other-user-id",
            IterationPath = ParentIterationPath,
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
