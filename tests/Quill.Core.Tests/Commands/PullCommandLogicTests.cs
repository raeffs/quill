using System.Net;
using Quill.AzureDevOps;
using Quill.Core.Markdown;
using Quill.Tests.Shared;
using Shouldly;

namespace Quill.Core.Tests.Commands;

public class PullCommandLogicTests
{
    [Fact]
    public async Task PullWorkflow_FetchesWorkItemAndProducesMarkdownFile()
    {
        var rawApiJson = """
        {
            "id": 42,
            "fields": {
                "System.WorkItemType": "Bug",
                "System.Title": "Fix login page",
                "System.State": "Active",
                "Microsoft.VSTS.TCM.ReproSteps": "<p>The login page is broken.</p>"
            }
        }
        """;

        using var handler = new FakeHttpHandler(rawApiJson, HttpStatusCode.OK);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsClient(httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        var workItem = await client.GetWorkItemAsync(42);
        var markdownBody = await MarkdownConverter.ToMarkdownAsync(
            workItem.Description, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project, client);
        var fileContent = FrontmatterParser.Write(
            id: workItem.Id,
            type: workItem.Type,
            title: workItem.Title,
            state: workItem.State,
            body: markdownBody.TrimEnd());

        var parsed = FrontmatterParser.Parse(fileContent);
        parsed.Id.ShouldBe(42);
        parsed.Title.ShouldBe("Fix login page");
        parsed.Type.ShouldBe("Bug");
        parsed.State.ShouldBe("Active");
        parsed.Body.ShouldContain("login page is broken");
        parsed.Body.ShouldNotContain("<p>");
    }

    [Fact]
    public async Task PullWorkflow_EmptyDescription_ProducesEmptyBody()
    {
        var rawApiJson = """
        {
            "id": 43,
            "fields": {
                "System.WorkItemType": "Task",
                "System.Title": "Empty task",
                "System.State": "New",
                "System.Description": ""
            }
        }
        """;

        using var handler = new FakeHttpHandler(rawApiJson, HttpStatusCode.OK);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsClient(httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        var workItem = await client.GetWorkItemAsync(43);

        var markdownBody = string.IsNullOrEmpty(workItem.Description)
            ? string.Empty
            : await MarkdownConverter.ToMarkdownAsync(
                workItem.Description, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project, client);

        var fileContent = FrontmatterParser.Write(
            id: workItem.Id,
            type: workItem.Type,
            title: workItem.Title,
            state: workItem.State,
            body: markdownBody.TrimEnd());

        var parsed = FrontmatterParser.Parse(fileContent);
        parsed.Id.ShouldBe(43);
        parsed.Body.ShouldBeEmpty();
    }
}

internal class FakeHttpHandler(string responseBody, HttpStatusCode statusCode) : HttpMessageHandler
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
