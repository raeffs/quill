using Microsoft.Extensions.Logging.Abstractions;
using Quill.Core.Actions;
using Quill.Core.Markdown;
using Quill.Core.Models;
using Quill.Tests.Shared;
using Shouldly;

namespace Quill.Core.Tests.Actions;

public class BacklogPullActionTests
{
    private static readonly QuillConfig Config = new()
    {
        ServerUrl = TestConstants.ServerUrl,
        Collection = TestConstants.Collection,
        Project = TestConstants.Project,
    };

    [Fact]
    public async Task RunAsync_WorkItemWithDescription_ReturnsMarkdownFile()
    {
        var client = new FakeAzureDevOpsClient();
        client.Add(WorkItem(42, "<p>Hello</p>"));

        var outcome = await BacklogPullAction.RunAsync(client, Config, NullLogger.Instance, 42);

        outcome.Id.ShouldBe(42);
        outcome.Title.ShouldBe("Fix login page");
        outcome.FileContent.ShouldContain("Hello");
        outcome.FileContent.ShouldContain("id: 42");
        outcome.FileContent.ShouldNotContain("<p>");
    }

    [Fact]
    public async Task RunAsync_EmptyDescription_ReturnsFileWithEmptyBody()
    {
        var client = new FakeAzureDevOpsClient();
        client.Add(WorkItem(43, string.Empty));

        var outcome = await BacklogPullAction.RunAsync(client, Config, NullLogger.Instance, 43);

        outcome.FileContent.ShouldContain("id: 43");
        FrontmatterParser.Parse(outcome.FileContent).Body.ShouldBeEmpty();
    }

    private static WorkItem WorkItem(int id, string description) => new()
    {
        Id = id,
        Type = "Bug",
        Title = "Fix login page",
        State = "Active",
        AssignedToId = string.Empty,
        Description = description,
    };
}
