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

        var ex = await Should.ThrowAsync<UsageException>(() => CreateAction.RunAsync(client, new IdentityResolver(new ScriptedIdentityClient(), TestPersonCache.Create()), new RichText(Config, client, NullLogger<RichText>.Instance), $"---\n{frontmatter}\n---\n\nbody\n", null));

        ex.Message.ShouldBe(message);
        client.Calls.ShouldBeEmpty();
    }

    [Fact]
    public async Task RunAsync_ParentAssignedToSomeoneElse_Throws()
    {
        var client = new FakeAzureDevOpsClient();
        client.Add(Parent(assignedToId: "other-user-id"));

        var ex = await Should.ThrowAsync<RejectedException>(() => CreateAction.RunAsync(client, new IdentityResolver(new ScriptedIdentityClient(), TestPersonCache.Create()), new RichText(Config, client, NullLogger<RichText>.Instance), ValidFile, null));

        ex.Message.ShouldBe("Parent work item assignee does not match the current user.");
        client.CreateCall.ShouldBeNull();
    }

    [Fact]
    public async Task RunAsync_ValidFile_CreatesUnderTheParentAndReturnsTheRewrittenFile()
    {
        var client = new FakeAzureDevOpsClient();
        client.Add(Parent());
        var identity = new ScriptedIdentityClient();

        var outcome = await CreateAction.RunAsync(client, new IdentityResolver(identity, TestPersonCache.Create()), new RichText(Config, client, NullLogger<RichText>.Instance), ValidFile, null);

        outcome.Row.Id.ShouldBe(1000);
        outcome.Row.Title.ShouldBe("T");
        outcome.FileContent.ShouldStartWith("---");
        outcome.FileContent.ShouldContain("id: 1000");
        client.CreateCall!.Assignee.ShouldBe(new Identity("user-1", "Jane Doe", null, false));
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

        await CreateAction.RunAsync(client, new IdentityResolver(identity, TestPersonCache.Create()), new RichText(Config, client, NullLogger<RichText>.Instance), ValidFile, "John Roe");

        client.CreateCall!.Assignee.ShouldBe(new Identity("id-2", "John Roe", @"CONTOSO\jroe", false));
    }

    [Fact]
    public async Task RunAsync_UnknownAssignee_CreatesNothing()
    {
        var client = new FakeAzureDevOpsClient();
        client.Add(Parent());

        await Should.ThrowAsync<RejectedException>(() => CreateAction.RunAsync(client, new IdentityResolver(new ScriptedIdentityClient(), TestPersonCache.Create()), new RichText(Config, client, NullLogger<RichText>.Instance), ValidFile, "Nobody"));

        client.CreateCall.ShouldBeNull();
    }

    [Fact]
    public async Task RunAsync_LinkRejectedAfterCreate_NamesTheCreatedItem()
    {
        var client = new FakeAzureDevOpsClient();
        client.Add(Parent());
        var rejected = new RejectedException("Work item 123 cannot be linked.");
        client.AddRelation = (_, _) => throw rejected;

        var ex = await Should.ThrowAsync<RejectedException>(() => CreateAction.RunAsync(client, new IdentityResolver(new ScriptedIdentityClient(), TestPersonCache.Create()), new RichText(Config, client, NullLogger<RichText>.Instance), LinkingFile, null));

        ex.Message.ShouldBe("Created work item 1000, but linking it failed: Work item 123 cannot be linked.");
        ex.InnerException.ShouldBeSameAs(rejected);
    }

    [Fact]
    public async Task RunAsync_AssigneeIsAGroup_ThrowsAndCreatesNothing()
    {
        var client = new FakeAzureDevOpsClient();
        client.Add(Parent());
        var identity = new ScriptedIdentityClient { Search = (_, _) => [new IdentityMatch("id-3", @"[Fabrikam]\Contoso Platform", null, true)] };

        var ex = await Should.ThrowAsync<RejectedException>(() => CreateAction.RunAsync(client, new IdentityResolver(identity, TestPersonCache.Create()), new RichText(Config, client, NullLogger<RichText>.Instance), ValidFile, "Contoso Platform"));

        ex.Message.ShouldBe("--assignee 'Contoso Platform' matches only groups. Groups are not accepted; name a person.");
        client.CreateCall.ShouldBeNull();
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
