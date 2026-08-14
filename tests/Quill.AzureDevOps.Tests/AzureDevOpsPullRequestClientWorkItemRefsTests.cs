using System.Net;
using Quill.Tests.Shared;
using Shouldly;

namespace Quill.AzureDevOps.Tests;

public class AzureDevOpsPullRequestClientWorkItemRefsTests
{
    [Fact]
    public async Task GetWorkItemRefsAsync_HitsRepoScopedWorkitemsEndpoint()
    {
        // Arrange
        using var handler = new FakeHttpHandler("""{"value":[]}""", HttpStatusCode.OK);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsPullRequestClient(
            httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        // Act
        var ids = await client.GetWorkItemRefsAsync(4711, "importer", TestContext.Current.CancellationToken);

        // Assert
        ids.ShouldBeEmpty();
        var url = handler.LastRequest!.RequestUri!.ToString();
        url.ShouldStartWith($"{TestConstants.ServerUrl}/{TestConstants.Collection}/{TestConstants.Project}/_apis/git/repositories/importer/pullRequests/4711/workitems?");
    }

    [Fact]
    public async Task GetWorkItemRefsAsync_ParsesIdsAsIntegers()
    {
        // Arrange
        var responseJson = """
        {
            "value": [
                {"id": "12345", "url": "https://server/_apis/wit/workItems/12345"},
                {"id": "67890", "url": "https://server/_apis/wit/workItems/67890"}
            ]
        }
        """;
        using var handler = new FakeHttpHandler(responseJson, HttpStatusCode.OK);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsPullRequestClient(
            httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        // Act
        var ids = await client.GetWorkItemRefsAsync(4711, "importer", TestContext.Current.CancellationToken);

        // Assert
        ids.ShouldBe([12345, 67890]);
    }

    [Fact]
    public async Task GetWorkItemRefsAsync_Non2xx_ThrowsHttpRequestException()
    {
        // Arrange
        using var handler = new FakeHttpHandler("""{"message":"nope"}""", HttpStatusCode.NotFound);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsPullRequestClient(
            httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        // Act
        Func<Task> act = () => client.GetWorkItemRefsAsync(4711, "importer", TestContext.Current.CancellationToken);

        // Assert
        await Should.ThrowAsync<HttpRequestException>(act);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetWorkItemRefsAsync_InvalidId_ThrowsArgumentOutOfRangeException(int id)
    {
        // Arrange
        using var handler = new FakeHttpHandler("{}", HttpStatusCode.OK);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsPullRequestClient(
            httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        // Act
        Func<Task> act = () => client.GetWorkItemRefsAsync(id, "importer", TestContext.Current.CancellationToken);

        // Assert
        await Should.ThrowAsync<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public async Task GetWorkItemRefsAsync_EmptyRepo_ThrowsArgumentException()
    {
        // Arrange
        using var handler = new FakeHttpHandler("{}", HttpStatusCode.OK);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsPullRequestClient(
            httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        // Act
        Func<Task> act = () => client.GetWorkItemRefsAsync(4711, string.Empty, TestContext.Current.CancellationToken);

        // Assert
        await Should.ThrowAsync<ArgumentException>(act);
    }

    [Fact]
    public async Task GetByIdAsync_IncludesDescriptionWhenPresent()
    {
        // Arrange
        var responseJson = """
        {
            "pullRequestId": 4711,
            "title": "Fix retry policy",
            "status": "active",
            "isDraft": false,
            "sourceRefName": "refs/heads/feat/retry",
            "targetRefName": "refs/heads/main",
            "creationDate": "2026-05-12T08:00:00Z",
            "createdBy": { "id": "u1", "displayName": "Jane Doe" },
            "repository": { "id": "r1", "name": "importer" },
            "reviewers": [],
            "description": "<p>Body</p>"
        }
        """;
        using var handler = new FakeHttpHandler(responseJson, HttpStatusCode.OK);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsPullRequestClient(
            httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        // Act
        var pr = await client.GetByIdAsync(4711, TestContext.Current.CancellationToken);

        // Assert
        pr.Description.ShouldBe("<p>Body</p>");
    }

    [Fact]
    public async Task GetByIdAsync_DescriptionMissing_UsesEmptyString()
    {
        // Arrange
        var responseJson = """
        {
            "pullRequestId": 4711,
            "title": "x",
            "status": "active",
            "isDraft": false,
            "sourceRefName": "refs/heads/feat/retry",
            "targetRefName": "refs/heads/main",
            "creationDate": "2026-05-12T08:00:00Z",
            "createdBy": { "id": "u1", "displayName": "Jane Doe" },
            "repository": { "id": "r1", "name": "importer" },
            "reviewers": []
        }
        """;
        using var handler = new FakeHttpHandler(responseJson, HttpStatusCode.OK);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsPullRequestClient(
            httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        // Act
        var pr = await client.GetByIdAsync(4711, TestContext.Current.CancellationToken);

        // Assert
        pr.Description.ShouldBe(string.Empty);
    }
}
