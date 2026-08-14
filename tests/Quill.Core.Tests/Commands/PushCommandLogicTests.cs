using System.Net;
using Quill.AzureDevOps;
using Quill.Core.Markdown;
using Quill.Core.Models;
using Quill.Core.Validation;
using Quill.Tests.Shared;
using Shouldly;

namespace Quill.Core.Tests.Commands;

public class PushCommandLogicTests
{
    [Fact]
    public async Task PushWorkflow_ValidFile_ProducesHtmlAndValidates()
    {
        var fileContent = FrontmatterParser.Write(
            id: 42,
            type: "Bug",
            title: "Updated Title",
            state: "Active",
            body: "Updated **description**.");

        var parsed = FrontmatterParser.Parse(fileContent);

        var workItemJson = $$"""
        {
            "id": 42,
            "fields": {
                "System.WorkItemType": "Bug",
                "System.Title": "Original Title",
                "System.State": "Active",
                "System.AssignedTo": { "id": "{{TestConstants.TestUserId}}" }
            }
        }
        """;

        using var handler = new FakePushHttpHandler(workItemJson, HttpStatusCode.OK);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsClient(httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        var workItem = await client.GetWorkItemAsync(parsed.Id);

        var config = new QuillConfig
        {
            ServerUrl = TestConstants.ServerUrl,
            Collection = TestConstants.Collection,
            Project = TestConstants.Project,
            AllowedStates = ["Active", "New"],
            AllowedParentStates = ["New", "Approved"],
        };

        var validation = PushValidator.Validate(workItem, config, TestConstants.TestUserId);
        validation.IsValid.ShouldBeTrue();

        var (html, linkIds) = await MarkdownConverter.ToHtmlAsync(
            parsed.Body, config.ServerUrl, config.Collection, config.Project, client);

        html.ShouldContain("<strong>description</strong>");
        linkIds.ShouldBeEmpty();
        parsed.Title.ShouldBe("Updated Title");
    }

    [Fact]
    public void PushWorkflow_InvalidState_FailsValidation()
    {
        var workItem = new WorkItem
        {
            Id = 42,
            Type = "Bug",
            Title = "Test",
            State = "Closed",
            AssignedToId = TestConstants.TestUserId,
        };

        var config = new QuillConfig
        {
            ServerUrl = TestConstants.ServerUrl,
            Collection = TestConstants.Collection,
            Project = TestConstants.Project,
            AllowedStates = ["Active", "New"],
            AllowedParentStates = ["New", "Approved"],
        };

        var validation = PushValidator.Validate(workItem, config, TestConstants.TestUserId);

        validation.IsValid.ShouldBeFalse();
        validation.Errors.ShouldContain(e => e.Contains("state"));
    }

    [Fact]
    public async Task PushWorkflow_WithWorkItemLinks_CollectsLinkIds()
    {
        var fileContent = FrontmatterParser.Write(
            id: 42,
            type: "Bug",
            title: "With Links",
            state: "Active",
            body: "See [related](#100) and [another](#200).");

        var parsed = FrontmatterParser.Parse(fileContent);

        var (_, linkIds) = await MarkdownConverter.ToHtmlAsync(
            parsed.Body, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        linkIds.ShouldBe([100, 200], ignoreOrder: true);
    }

    [Fact]
    public void PushWorkflow_NewRelationsFiltered_ExistingExcluded()
    {
        var existingRelatedIds = new HashSet<int> { 100, 300 };
        var linkIds = new List<int> { 100, 200, 400 };

        var newRelations = linkIds.Where(id => !existingRelatedIds.Contains(id)).ToList();

        newRelations.ShouldBe([200, 400]);
    }
}

internal class FakePushHttpHandler(string responseBody, HttpStatusCode statusCode) : HttpMessageHandler
{
    public HttpRequestMessage? LastRequest { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        return Task.FromResult(new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(responseBody, System.Text.Encoding.UTF8, "application/json"),
        });
    }
}
