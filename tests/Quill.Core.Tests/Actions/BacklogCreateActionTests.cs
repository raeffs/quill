using Microsoft.Extensions.Logging.Abstractions;
using Quill.Core.Actions;
using Quill.Core.Models;
using Quill.Tests.Shared;
using Quill.Tests.Shared.Fakes;
using Shouldly;

namespace Quill.Core.Tests.Actions;

public class BacklogCreateActionTests
{
    private const string ValidFile = "---\nid: 0\ntitle: T\ntype: Task\nparentId: 42\n---\n\nbody\n";

    private static readonly QuillConfig Config = new()
    {
        ServerUrl = TestConstants.ServerUrl,
        Collection = TestConstants.Collection,
        Project = TestConstants.Project,
        AllowedStates = ["New"],
    };

    [Theory]
    [InlineData("id: 5\ntitle: T\ntype: Task\nparentId: 42", "Work item id must be 0 for creation.")]
    [InlineData("id: 0\ntitle: ' '\ntype: Task\nparentId: 42", "Frontmatter must contain a non-empty 'title' field.")]
    [InlineData("id: 0\ntitle: T\nparentId: 42", "Frontmatter must contain a 'type' field.")]
    [InlineData("id: 0\ntitle: T\ntype: Epic\nparentId: 42", "Type must be one of: Feature, Product Backlog Item, Bug, Task.")]
    [InlineData("id: 0\ntitle: T\ntype: Task", "Frontmatter must contain a non-zero 'parentId' field.")]
    public async Task RunAsync_BadFrontmatter_ThrowsBeforeAnyRequest(string frontmatter, string message)
    {
        var client = new FakeAzureDevOpsClient();

        var ex = await Should.ThrowAsync<InvalidRequestException>(() => BacklogCreateAction.RunAsync(client, new ScriptedIdentityClient(), Config, NullLogger.Instance, $"---\n{frontmatter}\n---\n\nbody\n", null));

        ex.Message.ShouldBe(message);
        client.Calls.ShouldBeEmpty();
    }

    [Fact]
    public async Task RunAsync_ParentAssignedToSomeoneElse_Throws()
    {
        var client = new FakeAzureDevOpsClient();
        client.Add(Parent(assignedToId: "other-user-id"));

        var ex = await Should.ThrowAsync<InvalidRequestException>(() => BacklogCreateAction.RunAsync(client, new ScriptedIdentityClient(), Config, NullLogger.Instance, ValidFile, null));

        ex.Message.ShouldBe("Parent work item assignee does not match the current user.");
        client.CreateCall.ShouldBeNull();
    }

    [Fact]
    public async Task RunAsync_ValidFile_CreatesUnderTheParentAndReturnsTheRewrittenFile()
    {
        var client = new FakeAzureDevOpsClient();
        client.Add(Parent());

        var outcome = await BacklogCreateAction.RunAsync(client, new ScriptedIdentityClient(), Config, NullLogger.Instance, ValidFile, null);

        outcome.Row.Id.ShouldBe(1000);
        outcome.Row.Title.ShouldBe("T");
        outcome.FileContent.ShouldStartWith("---");
        outcome.FileContent.ShouldContain("id: 1000");
        client.CreateCall!.AssignedToId.ShouldBe("Jane Doe");
    }

    [Fact]
    public async Task RunAsync_AssignedTo_OverridesTheCurrentUser()
    {
        var client = new FakeAzureDevOpsClient();
        client.Add(Parent());

        await BacklogCreateAction.RunAsync(client, new ScriptedIdentityClient(), Config, NullLogger.Instance, ValidFile, "John Roe");

        client.CreateCall!.AssignedToId.ShouldBe("John Roe");
    }

    private static WorkItem Parent(string assignedToId = "user-1") => new()
    {
        Id = 42,
        Type = "Product Backlog Item",
        Title = "The parent",
        State = "New",
        AssignedToId = assignedToId,
    };
}
