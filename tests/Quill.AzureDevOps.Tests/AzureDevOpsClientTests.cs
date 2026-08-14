using System.Net;
using Quill.Tests.Shared;
using Shouldly;

namespace Quill.AzureDevOps.Tests;

public class AzureDevOpsClientTests
{
    [Fact]
    public async Task GetWorkItemAsync_ValidResponse_ReturnsMappedWorkItem()
    {
        // Use raw JSON to accurately simulate real API responses (avoids reflection serialization asymmetry)
        var rawApiJson = $$"""
        {
            "id": 123,
            "fields": {
                "System.WorkItemType": "Bug",
                "System.Title": "Fix login",
                "System.State": "Active",
                "System.AssignedTo": { "id": "44892788-c082-4795-a323-8cc6daaaaba2", "displayName": "John Doe", "uniqueName": "john@example.com" },
                "Microsoft.VSTS.TCM.ReproSteps": "<p>Fix the login bug</p>"
            },
            "relations": [
                {
                    "rel": "System.LinkTypes.Related",
                    "url": "{{TestConstants.ServerUrl}}/{{TestConstants.Collection}}/_apis/wit/workitems/456"
                }
            ]
        }
        """;

        using var handler = new FakeHttpHandler(rawApiJson, HttpStatusCode.OK);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsClient(httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        var workItem = await client.GetWorkItemAsync(123);

        workItem.Id.ShouldBe(123);
        workItem.Type.ShouldBe("Bug");
        workItem.Title.ShouldBe("Fix login");
        workItem.State.ShouldBe("Active");
        workItem.AssignedToId.ShouldBe("44892788-c082-4795-a323-8cc6daaaaba2");
        workItem.AssignedToDisplayName.ShouldBe("John Doe");
        workItem.Description.ShouldBe("<p>Fix the login bug</p>");
        workItem.Relations.ShouldHaveSingleItem().TargetId.ShouldBe(456);
    }

    [Fact]
    public async Task GetWorkItemAsync_Unassigned_ReturnsEmptyAssignedToDisplayName()
    {
        var rawApiJson = """
        {
            "id": 77,
            "fields": {
                "System.WorkItemType": "Task",
                "System.Title": "Unassigned",
                "System.State": "New",
                "System.Description": ""
            },
            "relations": []
        }
        """;

        using var handler = new FakeHttpHandler(rawApiJson, HttpStatusCode.OK);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsClient(httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        var workItem = await client.GetWorkItemAsync(77);

        workItem.AssignedToId.ShouldBeEmpty();
        workItem.AssignedToDisplayName.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetWorkItemAsync_RawApiJson_DescriptionIsCorrectHtml()
    {
        // Bug-type work items store their body in Microsoft.VSTS.TCM.ReproSteps.
        var rawApiJson = """
        {
            "id": 42,
            "fields": {
                "System.WorkItemType": "Bug",
                "System.Title": "Test work item",
                "System.State": "Active",
                "Microsoft.VSTS.TCM.ReproSteps": "<p>This is <b>HTML</b> content</p>"
            },
            "relations": []
        }
        """;

        using var handler = new FakeHttpHandler(rawApiJson, HttpStatusCode.OK);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsClient(httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        var workItem = await client.GetWorkItemAsync(42);

        workItem.Description.ShouldBe("<p>This is <b>HTML</b> content</p>");
    }

    [Fact]
    public async Task GetWorkItemAsync_RawApiJson_DescriptionConvertibleToMarkdown()
    {
        var rawApiJson = """
        {
            "id": 42,
            "fields": {
                "System.WorkItemType": "Bug",
                "System.Title": "Test work item",
                "System.State": "Active",
                "Microsoft.VSTS.TCM.ReproSteps": "<p>This is <b>HTML</b> content</p>"
            },
            "relations": []
        }
        """;

        using var handler = new FakeHttpHandler(rawApiJson, HttpStatusCode.OK);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsClient(httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        var workItem = await client.GetWorkItemAsync(42);

        var markdown = await Quill.Core.Markdown.MarkdownConverter.ToMarkdownAsync(
            workItem.Description, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        markdown.ShouldContain("**HTML**");
        markdown.ShouldNotContain("<p>");
        markdown.ShouldNotContain("<b>");
    }

    [Fact]
    public async Task GetWorkItemAsync_WithChildRelations_ReturnsChildIds()
    {
        var rawApiJson = $$"""
        {
            "id": 100,
            "fields": {
                "System.WorkItemType": "Product Backlog Item",
                "System.Title": "Parent PBI",
                "System.State": "New",
                "System.Description": ""
            },
            "relations": [
                {
                    "rel": "System.LinkTypes.Hierarchy-Forward",
                    "url": "{{TestConstants.ServerUrl}}/{{TestConstants.Collection}}/_apis/wit/workitems/201"
                },
                {
                    "rel": "System.LinkTypes.Hierarchy-Forward",
                    "url": "{{TestConstants.ServerUrl}}/{{TestConstants.Collection}}/_apis/wit/workitems/202"
                },
                {
                    "rel": "System.LinkTypes.Related",
                    "url": "{{TestConstants.ServerUrl}}/{{TestConstants.Collection}}/_apis/wit/workitems/300"
                }
            ]
        }
        """;

        using var handler = new FakeHttpHandler(rawApiJson, HttpStatusCode.OK);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsClient(httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        var workItem = await client.GetWorkItemAsync(100);

        workItem.ChildIds.ShouldBe([201, 202]);
    }

    [Fact]
    public async Task GetWorkItemAsync_Non2xx_ThrowsHttpRequestException()
    {
        using var handler = new FakeHttpHandler("""{"message":"Not found"}""", HttpStatusCode.NotFound);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsClient(httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        Func<Task> act = () => client.GetWorkItemAsync(999);

        await Should.ThrowAsync<HttpRequestException>(act);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetWorkItemAsync_InvalidId_ThrowsArgumentOutOfRangeException(int id)
    {
        using var handler = new FakeHttpHandler("{}", HttpStatusCode.OK);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsClient(httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        Func<Task> act = () => client.GetWorkItemAsync(id);

        await Should.ThrowAsync<ArgumentOutOfRangeException>(act);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task UpdateWorkItemFieldsAsync_InvalidId_ThrowsArgumentOutOfRangeException(int id)
    {
        using var handler = new FakeHttpHandler("{}", HttpStatusCode.OK);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsClient(httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        Func<Task> act = () => client.UpdateWorkItemFieldsAsync(id, "Product Backlog Item", "title", "<p>desc</p>");

        await Should.ThrowAsync<ArgumentOutOfRangeException>(act);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task AddRelationAsync_InvalidSourceId_ThrowsArgumentOutOfRangeException(int sourceId)
    {
        using var handler = new FakeHttpHandler("{}", HttpStatusCode.OK);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsClient(httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        Func<Task> act = () => client.AddRelationAsync(sourceId, 1);

        await Should.ThrowAsync<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public async Task UpdateWorkItemFieldsAsync_NonBug_PatchesSystemDescription()
    {
        // Arrange
        using var handler = new FakeHttpHandler("{}", HttpStatusCode.OK);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsClient(httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        // Act
        await client.UpdateWorkItemFieldsAsync(123, "Product Backlog Item", "New Title", "<p>New Description</p>");

        // Assert
        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.Method.ShouldBe(HttpMethod.Patch);
        handler.LastRequest.RequestUri!.ToString().ShouldContain("/workitems/123");

        handler.LastRequestBody.ShouldNotBeNull();
        handler.LastRequestBody.ShouldContain("System.Title");
        handler.LastRequestBody.ShouldContain("New Title");
        handler.LastRequestBody.ShouldContain("System.Description");
        handler.LastRequestBody.ShouldContain("New Description");
        handler.LastRequestBody.ShouldNotContain("Microsoft.VSTS.TCM.ReproSteps");
    }

    [Fact]
    public async Task UpdateWorkItemFieldsAsync_Bug_PatchesReproSteps()
    {
        // Arrange
        using var handler = new FakeHttpHandler("{}", HttpStatusCode.OK);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsClient(httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        // Act
        await client.UpdateWorkItemFieldsAsync(123, "Bug", "New Title", "<p>New Description</p>");

        // Assert
        handler.LastRequestBody.ShouldNotBeNull();
        handler.LastRequestBody.ShouldContain("System.Title");
        handler.LastRequestBody.ShouldContain("New Title");
        handler.LastRequestBody.ShouldContain("Microsoft.VSTS.TCM.ReproSteps");
        handler.LastRequestBody.ShouldContain("New Description");
        handler.LastRequestBody.ShouldNotContain("System.Description");
    }

    [Fact]
    public async Task GetWorkItemAsync_WithParentRelation_ReturnsParentId()
    {
        var rawApiJson = $$"""
        {
            "id": 200,
            "fields": {
                "System.WorkItemType": "Task",
                "System.Title": "Child task",
                "System.State": "New",
                "System.Description": ""
            },
            "relations": [
                {
                    "rel": "System.LinkTypes.Hierarchy-Reverse",
                    "url": "{{TestConstants.ServerUrl}}/{{TestConstants.Collection}}/_apis/wit/workitems/100"
                }
            ]
        }
        """;

        using var handler = new FakeHttpHandler(rawApiJson, HttpStatusCode.OK);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsClient(httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        var workItem = await client.GetWorkItemAsync(200);

        workItem.ParentId.ShouldBe(100);
    }

    [Fact]
    public async Task GetWorkItemAsync_WithoutParentRelation_ReturnsNullParentId()
    {
        var rawApiJson = """
        {
            "id": 200,
            "fields": {
                "System.WorkItemType": "Bug",
                "System.Title": "No parent",
                "System.State": "Active",
                "System.Description": ""
            },
            "relations": []
        }
        """;

        using var handler = new FakeHttpHandler(rawApiJson, HttpStatusCode.OK);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsClient(httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        var workItem = await client.GetWorkItemAsync(200);

        workItem.ParentId.ShouldBeNull();
    }

    [Fact]
    public async Task AddRelationAsync_SendsCorrectPatchOperation()
    {
        using var handler = new FakeHttpHandler("{}", HttpStatusCode.OK);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsClient(httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        await client.AddRelationAsync(100, 200);

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.Method.ShouldBe(HttpMethod.Patch);
        handler.LastRequest.RequestUri!.ToString().ShouldContain("/workitems/100");

        handler.LastRequestBody.ShouldNotBeNull();
        handler.LastRequestBody.ShouldContain("System.LinkTypes.Related");
        handler.LastRequestBody.ShouldContain("/relations/-");
        handler.LastRequestBody.ShouldContain("/workitems/200");
    }
    [Fact]
    public async Task CreateWorkItemAsync_SendsCorrectPatchOperations()
    {
        var responseJson = """{"id": 999, "fields": {}}""";

        using var handler = new FakeHttpHandler(responseJson, HttpStatusCode.OK);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsClient(httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        var newId = await client.CreateWorkItemAsync("Bug", "New bug", 100, "44892788-c082-4795-a323-8cc6daaaaba2");

        newId.ShouldBe(999);
        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.Method.ShouldBe(HttpMethod.Patch);
        handler.LastRequest.RequestUri!.ToString().ShouldContain("/workitems/$Bug");

        handler.LastRequestBody.ShouldNotBeNull();
        handler.LastRequestBody.ShouldContain("System.Title");
        handler.LastRequestBody.ShouldContain("New bug");
        handler.LastRequestBody.ShouldNotContain("System.State");
        handler.LastRequestBody.ShouldContain("System.AssignedTo");
        handler.LastRequestBody.ShouldContain("44892788-c082-4795-a323-8cc6daaaaba2");
        handler.LastRequestBody.ShouldContain("System.LinkTypes.Hierarchy-Reverse");
        handler.LastRequestBody.ShouldContain("/workitems/100");
    }

    [Fact]
    public async Task CreateWorkItemAsync_WithoutAssignedTo_DoesNotSendAssignedToField()
    {
        var responseJson = """{"id": 999, "fields": {}}""";

        using var handler = new FakeHttpHandler(responseJson, HttpStatusCode.OK);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsClient(httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        var newId = await client.CreateWorkItemAsync("Task", "New task", 100);

        newId.ShouldBe(999);
        handler.LastRequestBody.ShouldNotBeNull();
        handler.LastRequestBody.ShouldNotContain("System.AssignedTo");
    }

    [Fact]
    public async Task CreateWorkItemAsync_NonBug_WithDescription_PatchesSystemDescription()
    {
        // Arrange
        var responseJson = """{"id": 999, "fields": {}}""";

        using var handler = new FakeHttpHandler(responseJson, HttpStatusCode.OK);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsClient(httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        // Act
        var newId = await client.CreateWorkItemAsync(
            "Product Backlog Item", "New PBI", 100, assignedToId: null, descriptionHtml: "<p>Body text</p>");

        // Assert
        newId.ShouldBe(999);
        handler.LastRequestBody.ShouldNotBeNull();
        handler.LastRequestBody.ShouldContain("System.Description");
        handler.LastRequestBody.ShouldContain("Body text");
        handler.LastRequestBody.ShouldNotContain("Microsoft.VSTS.TCM.ReproSteps");
    }

    [Fact]
    public async Task CreateWorkItemAsync_Bug_WithDescription_PatchesReproSteps()
    {
        // Arrange
        var responseJson = """{"id": 999, "fields": {}}""";

        using var handler = new FakeHttpHandler(responseJson, HttpStatusCode.OK);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsClient(httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        // Act
        var newId = await client.CreateWorkItemAsync(
            "Bug", "New bug", 100, assignedToId: null, descriptionHtml: "<p>Body text</p>");

        // Assert
        newId.ShouldBe(999);
        handler.LastRequestBody.ShouldNotBeNull();
        handler.LastRequestBody.ShouldContain("Microsoft.VSTS.TCM.ReproSteps");
        handler.LastRequestBody.ShouldContain("Body text");
        handler.LastRequestBody.ShouldNotContain("System.Description");
    }

    [Fact]
    public async Task CreateWorkItemAsync_WithoutDescription_OmitsDescriptionPatchOp()
    {
        var responseJson = """{"id": 999, "fields": {}}""";

        using var handler = new FakeHttpHandler(responseJson, HttpStatusCode.OK);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsClient(httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        await client.CreateWorkItemAsync("Task", "New task", 100);

        handler.LastRequestBody.ShouldNotBeNull();
        handler.LastRequestBody.ShouldNotContain("System.Description");
    }

    [Fact]
    public async Task GetWorkItemAsync_WithIterationPath_ReturnsIterationPath()
    {
        // Arrange
        var rawApiJson = """
        {
            "id": 321,
            "fields": {
                "System.WorkItemType": "Product Backlog Item",
                "System.Title": "Item in sprint",
                "System.State": "Approved",
                "System.Description": "",
                "System.IterationPath": "MyProject\\Sprint 42"
            },
            "relations": []
        }
        """;

        using var handler = new FakeHttpHandler(rawApiJson, HttpStatusCode.OK);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsClient(httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        // Act
        var workItem = await client.GetWorkItemAsync(321);

        // Assert
        workItem.IterationPath.ShouldBe("MyProject\\Sprint 42");
    }

    [Fact]
    public async Task GetWorkItemAsync_WithoutIterationPath_ReturnsEmptyIterationPath()
    {
        // Arrange
        var rawApiJson = """
        {
            "id": 322,
            "fields": {
                "System.WorkItemType": "Bug",
                "System.Title": "No iteration",
                "System.State": "Active",
                "System.Description": ""
            },
            "relations": []
        }
        """;

        using var handler = new FakeHttpHandler(rawApiJson, HttpStatusCode.OK);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsClient(httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        // Act
        var workItem = await client.GetWorkItemAsync(322);

        // Assert
        workItem.IterationPath.ShouldBeEmpty();
    }

    [Fact]
    public async Task CreateWorkItemAsync_WithIterationPath_IncludesIterationPathPatchOp()
    {
        // Arrange
        var responseJson = """{"id": 999, "fields": {}}""";

        using var handler = new FakeHttpHandler(responseJson, HttpStatusCode.OK);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsClient(httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        // Act
        await client.CreateWorkItemAsync(
            "Task", "Sprint task", 100, assignedToId: null, descriptionHtml: null, iterationPath: "MyProject\\Sprint 42");

        // Assert
        handler.LastRequestBody.ShouldNotBeNull();
        handler.LastRequestBody.ShouldContain("System.IterationPath");
        handler.LastRequestBody.ShouldContain("Sprint 42");
    }

    [Fact]
    public async Task GetWorkItemAsync_Bug_IgnoresSystemDescription()
    {
        // Arrange — strict swap: a Bug's System.Description is not read, even if populated.
        var rawApiJson = """
        {
            "id": 50,
            "fields": {
                "System.WorkItemType": "Bug",
                "System.Title": "Bug with legacy description",
                "System.State": "Active",
                "System.Description": "<p>legacy content in wrong field</p>",
                "Microsoft.VSTS.TCM.ReproSteps": ""
            },
            "relations": []
        }
        """;

        using var handler = new FakeHttpHandler(rawApiJson, HttpStatusCode.OK);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsClient(httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        // Act
        var workItem = await client.GetWorkItemAsync(50);

        // Assert
        workItem.Description.ShouldBeEmpty();
    }

    [Fact]
    public async Task CreateWorkItemAsync_WithoutIterationPath_OmitsIterationPathPatchOp()
    {
        // Arrange
        var responseJson = """{"id": 999, "fields": {}}""";

        using var handler = new FakeHttpHandler(responseJson, HttpStatusCode.OK);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsClient(httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        // Act
        await client.CreateWorkItemAsync("Task", "No-iteration task", 100);

        // Assert
        handler.LastRequestBody.ShouldNotBeNull();
        handler.LastRequestBody.ShouldNotContain("System.IterationPath");
    }
}

internal class FakeHttpHandler(string responseBody, HttpStatusCode statusCode) : HttpMessageHandler
{
    public HttpRequestMessage? LastRequest { get; private set; }
    public string? LastRequestBody { get; private set; }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        if (request.Content is not null)
        {
            LastRequestBody = await request.Content.ReadAsStringAsync(cancellationToken);
        }

        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(responseBody, System.Text.Encoding.UTF8, "application/json"),
        };
    }
}
