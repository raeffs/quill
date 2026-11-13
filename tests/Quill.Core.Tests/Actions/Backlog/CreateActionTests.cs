using Microsoft.Extensions.Logging.Abstractions;
using Quill.Core.Actions.Backlog;
using Quill.Core.Markdown;
using Quill.Core.Models;
using Quill.Tests.Shared;
using Quill.Tests.Shared.Fakes;
using Shouldly;

namespace Quill.Core.Tests.Actions.Backlog;

public class CreateActionTests
{
    private const string ValidFile = "---\nid: 0\ntitle: T\ntype: Task\nparentId: 42\n---\n\nbody\n";
    private const string LinkingFile = "---\nid: 0\ntitle: T\ntype: Task\nparentId: 42\n---\n\nSee [the bug](#123).\n";

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
    [InlineData("id: 0\ntitle: T\ntype: Task", "Frontmatter must contain a positive 'parentId' field.")]
    [InlineData("id: 0\ntitle: T\ntype: Task\nparentId: 0", "Frontmatter must contain a positive 'parentId' field.")]
    [InlineData("id: 0\ntitle: T\ntype: Task\nparentId: -1", "Frontmatter must contain a positive 'parentId' field.")]
    public async Task RunAsync_BadFrontmatter_ThrowsBeforeAnyRequest(string frontmatter, string message)
    {
        var client = new FakeAzureDevOpsClient();

        var ex = await Should.ThrowAsync<UsageException>(() => CreateAction.RunAsync(client, new ScriptedIdentityClient(), new RichText(Config, client, NullLogger<RichText>.Instance), $"---\n{frontmatter}\n---\n\nbody\n", null));

        ex.Message.ShouldBe(message);
        client.Calls.ShouldBeEmpty();
    }

    [Fact]
    public async Task RunAsync_ParentAssignedToSomeoneElse_Throws()
    {
        var client = new FakeAzureDevOpsClient();
        client.Add(Parent(assignedToId: "other-user-id"));

        var ex = await Should.ThrowAsync<RejectedException>(() => CreateAction.RunAsync(client, new ScriptedIdentityClient(), new RichText(Config, client, NullLogger<RichText>.Instance), ValidFile, null));

        ex.Message.ShouldBe("Parent work item assignee does not match the current user.");
        client.CreateCall.ShouldBeNull();
    }

    [Fact]
    public async Task RunAsync_ValidFile_CreatesUnderTheParentAndReturnsTheRewrittenFile()
    {
        var client = new FakeAzureDevOpsClient();
        client.Add(Parent());
        var identity = new ScriptedIdentityClient();

        var outcome = await CreateAction.RunAsync(client, identity, new RichText(Config, client, NullLogger<RichText>.Instance), ValidFile, null);

        outcome.Row.Id.ShouldBe(1000);
        outcome.Row.Title.ShouldBe("T");
        outcome.FileContent.ShouldStartWith("---");
        outcome.FileContent.ShouldContain("id: 1000");
        client.CreateCall!.AssignedToId.ShouldBe("Jane Doe");
        identity.Calls.ShouldBe(["GetCurrentUser"]);
    }

    [Fact]
    public async Task RunAsync_NamedAssignee_WritesTheCombinedForm()
    {
        var client = new FakeAzureDevOpsClient();
        client.Add(Parent());
        var identity = new ScriptedIdentityClient
        {
            Search = (_, _) => [new IdentityMatch("id-2", "John Roe", @"CONTOSO\jroe", false)],
        };

        await CreateAction.RunAsync(client, identity, new RichText(Config, client, NullLogger<RichText>.Instance), ValidFile, "John Roe");

        client.CreateCall!.AssignedToId.ShouldBe(@"John Roe <CONTOSO\jroe>");
    }

    [Fact]
    public async Task RunAsync_UnknownAssignee_CreatesNothing()
    {
        var client = new FakeAzureDevOpsClient();
        client.Add(Parent());

        await Should.ThrowAsync<RejectedException>(() => CreateAction.RunAsync(client, new ScriptedIdentityClient(), new RichText(Config, client, NullLogger<RichText>.Instance), ValidFile, "Nobody"));

        client.CreateCall.ShouldBeNull();
    }

    [Fact]
    public async Task RunAsync_LinkRejectedAfterCreate_NamesTheCreatedItem()
    {
        var client = new FakeAzureDevOpsClient();
        client.Add(Parent());
        var rejected = new RejectedException("Work item 123 cannot be linked.");
        client.AddRelation = (_, _) => throw rejected;

        var ex = await Should.ThrowAsync<RejectedException>(() => CreateAction.RunAsync(client, new ScriptedIdentityClient(), new RichText(Config, client, NullLogger<RichText>.Instance), LinkingFile, null));

        ex.Message.ShouldBe("Created work item 1000, but linking it failed: Work item 123 cannot be linked.");
        ex.InnerException.ShouldBeSameAs(rejected);
    }

    [Fact]
    public async Task RunAsync_LinkUnavailableAfterCreate_KeepsRetryAfter()
    {
        var client = new FakeAzureDevOpsClient();
        client.Add(Parent());
        var unavailable = new UnavailableException("The server is busy.", TimeSpan.FromSeconds(30));
        client.AddRelation = (_, _) => throw unavailable;

        var ex = await Should.ThrowAsync<UnavailableException>(() => CreateAction.RunAsync(client, new ScriptedIdentityClient(), new RichText(Config, client, NullLogger<RichText>.Instance), LinkingFile, null));

        ex.Message.ShouldBe("Created work item 1000, but linking it failed: The server is busy.");
        ex.RetryAfter.ShouldBe(TimeSpan.FromSeconds(30));
        ex.InnerException.ShouldBeSameAs(unavailable);
    }

    [Fact]
    public async Task RunAsync_DefectAfterCreate_PassesThroughUnchanged()
    {
        var client = new FakeAzureDevOpsClient();
        client.Add(Parent());
        var defect = new DefectException("Failed to deserialize work item response.");
        client.AddRelation = (_, _) => throw defect;

        var ex = await Should.ThrowAsync<DefectException>(() => CreateAction.RunAsync(client, new ScriptedIdentityClient(), new RichText(Config, client, NullLogger<RichText>.Instance), LinkingFile, null));

        ex.ShouldBeSameAs(defect);
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
