using System.Net;
using Quill.Tests.Shared;
using Shouldly;

namespace Quill.AzureDevOps.Tests;

public class AzureDevOpsPullRequestClientThreadsTests
{
    [Fact]
    public async Task GetByIdAsync_HitsProjectScopedPrEndpointAndMapsRepo()
    {
        // Arrange
        var responseJson = $$"""
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
        pr.Id.ShouldBe(4711);
        pr.RepoName.ShouldBe("importer");
        var url = handler.LastRequest!.RequestUri!.ToString();
        url.ShouldStartWith($"{TestConstants.ServerUrl}/{TestConstants.Collection}/{TestConstants.Project}/_apis/git/pullrequests/4711?");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetByIdAsync_InvalidId_ThrowsArgumentOutOfRangeException(int id)
    {
        // Arrange
        using var handler = new FakeHttpHandler("{}", HttpStatusCode.OK);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsPullRequestClient(
            httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        // Act
        Func<Task> act = () => client.GetByIdAsync(id, TestContext.Current.CancellationToken);

        // Assert
        await Should.ThrowAsync<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public async Task GetThreadsAsync_HitsRepoScopedThreadsEndpoint()
    {
        // Arrange
        using var handler = new FakeHttpHandler("""{"value":[]}""", HttpStatusCode.OK);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsPullRequestClient(
            httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        // Act
        var threads = await client.GetThreadsAsync(4711, "importer", TestContext.Current.CancellationToken);

        // Assert
        threads.ShouldBeEmpty();
        var url = handler.LastRequest!.RequestUri!.ToString();
        url.ShouldStartWith($"{TestConstants.ServerUrl}/{TestConstants.Collection}/{TestConstants.Project}/_apis/git/repositories/importer/pullRequests/4711/threads?");
    }

    [Fact]
    public async Task GetThreadsAsync_MapsFileScopedRightSideThread()
    {
        // Arrange
        var responseJson = """
        {
            "value": [
                {
                    "id": 88123,
                    "status": "active",
                    "publishedDate": "2026-05-13T09:00:00Z",
                    "threadContext": {
                        "filePath": "/src/Importer/Retry.cs",
                        "rightFileStart": { "line": 42, "offset": 1 },
                        "rightFileEnd": { "line": 42, "offset": 12 }
                    },
                    "comments": [
                        {
                            "id": 1,
                            "content": "Consider exponential backoff here.",
                            "author": { "displayName": "John Roe" },
                            "publishedDate": "2026-05-13T09:00:00Z",
                            "lastContentUpdatedDate": "2026-05-13T09:00:00Z"
                        }
                    ]
                }
            ]
        }
        """;
        using var handler = new FakeHttpHandler(responseJson, HttpStatusCode.OK);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsPullRequestClient(
            httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        // Act
        var threads = await client.GetThreadsAsync(4711, "importer", TestContext.Current.CancellationToken);

        // Assert
        var thread = threads.ShouldHaveSingleItem();
        thread.Id.ShouldBe(88123);
        thread.Status.ShouldBe("active");
        thread.FilePath.ShouldBe("src/Importer/Retry.cs");
        thread.Side.ShouldBe("right");
        thread.StartLine.ShouldBe(42);
        thread.EndLine.ShouldBe(42);
        thread.Comments.Count.ShouldBe(1);
        thread.Comments[0].Id.ShouldBe(1);
        thread.Comments[0].Author.ShouldBe("John Roe");
        thread.Comments[0].ModifiedDate.ShouldBeNull();
        thread.Comments[0].TextHtml.ShouldBe("Consider exponential backoff here.");
    }

    [Fact]
    public async Task GetThreadsAsync_LeftSideThread_MapsToLeft()
    {
        // Arrange
        var responseJson = """
        {
            "value": [
                {
                    "id": 88130,
                    "status": "active",
                    "publishedDate": "2026-05-13T10:00:00Z",
                    "threadContext": {
                        "filePath": "/src/Importer/Old.cs",
                        "leftFileStart": { "line": 18, "offset": 1 },
                        "leftFileEnd": { "line": 22, "offset": 1 }
                    },
                    "comments": [
                        {
                            "id": 5,
                            "content": "Was this validation removed intentionally?",
                            "author": { "displayName": "John Roe" },
                            "publishedDate": "2026-05-13T10:00:00Z"
                        }
                    ]
                }
            ]
        }
        """;
        using var handler = new FakeHttpHandler(responseJson, HttpStatusCode.OK);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsPullRequestClient(
            httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        // Act
        var threads = await client.GetThreadsAsync(4711, "importer", TestContext.Current.CancellationToken);

        // Assert
        var thread = threads.ShouldHaveSingleItem();
        thread.Side.ShouldBe("left");
        thread.StartLine.ShouldBe(18);
        thread.EndLine.ShouldBe(22);
        thread.FilePath.ShouldBe("src/Importer/Old.cs");
    }

    [Fact]
    public async Task GetThreadsAsync_OverallThread_AllLocationFieldsNull()
    {
        // Arrange
        var responseJson = """
        {
            "value": [
                {
                    "id": 88200,
                    "status": "active",
                    "publishedDate": "2026-05-13T11:00:00Z",
                    "threadContext": null,
                    "comments": [
                        {
                            "id": 9,
                            "content": "Overall, LGTM.",
                            "author": { "displayName": "Jane Doe" },
                            "publishedDate": "2026-05-13T11:00:00Z",
                            "lastContentUpdatedDate": "2026-05-13T11:05:00Z"
                        }
                    ]
                }
            ]
        }
        """;
        using var handler = new FakeHttpHandler(responseJson, HttpStatusCode.OK);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsPullRequestClient(
            httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        // Act
        var threads = await client.GetThreadsAsync(4711, "importer", TestContext.Current.CancellationToken);

        // Assert
        var thread = threads.ShouldHaveSingleItem();
        thread.FilePath.ShouldBeNull();
        thread.Side.ShouldBeNull();
        thread.StartLine.ShouldBeNull();
        thread.EndLine.ShouldBeNull();
        thread.Comments[0].ModifiedDate.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetThreadsAsync_DeletedThread_IsFiltered()
    {
        // Arrange
        var responseJson = """
        {
            "value": [
                {
                    "id": 1,
                    "status": "closed",
                    "isDeleted": true,
                    "publishedDate": "2026-05-13T09:00:00Z",
                    "comments": [
                        { "id": 1, "content": "x", "author": { "displayName": "u" }, "publishedDate": "2026-05-13T09:00:00Z" }
                    ]
                },
                {
                    "id": 2,
                    "status": "active",
                    "publishedDate": "2026-05-13T10:00:00Z",
                    "comments": [
                        { "id": 2, "content": "kept", "author": { "displayName": "u" }, "publishedDate": "2026-05-13T10:00:00Z" }
                    ]
                }
            ]
        }
        """;
        using var handler = new FakeHttpHandler(responseJson, HttpStatusCode.OK);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsPullRequestClient(
            httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        // Act
        var threads = await client.GetThreadsAsync(4711, "importer", TestContext.Current.CancellationToken);

        // Assert
        threads.ShouldHaveSingleItem().Id.ShouldBe(2);
    }

    [Fact]
    public async Task GetThreadsAsync_SystemThread_IsFiltered()
    {
        // Arrange
        var responseJson = """
        {
            "value": [
                {
                    "id": 1,
                    "status": "closed",
                    "publishedDate": "2026-05-13T09:00:00Z",
                    "properties": {
                        "CodeReviewThreadType": { "$type": "System.String", "$value": "VoteUpdate" }
                    },
                    "comments": [
                        { "id": 1, "content": "approved", "author": { "displayName": "u" }, "publishedDate": "2026-05-13T09:00:00Z" }
                    ]
                },
                {
                    "id": 2,
                    "status": "active",
                    "publishedDate": "2026-05-13T10:00:00Z",
                    "comments": [
                        { "id": 2, "content": "kept", "author": { "displayName": "u" }, "publishedDate": "2026-05-13T10:00:00Z" }
                    ]
                }
            ]
        }
        """;
        using var handler = new FakeHttpHandler(responseJson, HttpStatusCode.OK);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsPullRequestClient(
            httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        // Act
        var threads = await client.GetThreadsAsync(4711, "importer", TestContext.Current.CancellationToken);

        // Assert
        threads.ShouldHaveSingleItem().Id.ShouldBe(2);
    }

    [Fact]
    public async Task GetThreadsAsync_ThreadWithAllDeletedComments_DropsOut()
    {
        // Arrange
        var responseJson = """
        {
            "value": [
                {
                    "id": 1,
                    "status": "active",
                    "publishedDate": "2026-05-13T09:00:00Z",
                    "comments": [
                        { "id": 1, "content": "x", "author": { "displayName": "u" }, "publishedDate": "2026-05-13T09:00:00Z", "isDeleted": true }
                    ]
                }
            ]
        }
        """;
        using var handler = new FakeHttpHandler(responseJson, HttpStatusCode.OK);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsPullRequestClient(
            httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        // Act
        var threads = await client.GetThreadsAsync(4711, "importer", TestContext.Current.CancellationToken);

        // Assert
        threads.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetThreadsAsync_DeletedComments_AreFilteredWithinThread()
    {
        // Arrange
        var responseJson = """
        {
            "value": [
                {
                    "id": 1,
                    "status": "active",
                    "publishedDate": "2026-05-13T09:00:00Z",
                    "comments": [
                        { "id": 1, "content": "kept", "author": { "displayName": "u" }, "publishedDate": "2026-05-13T09:00:00Z" },
                        { "id": 2, "content": "gone", "author": { "displayName": "u" }, "publishedDate": "2026-05-13T09:05:00Z", "isDeleted": true }
                    ]
                }
            ]
        }
        """;
        using var handler = new FakeHttpHandler(responseJson, HttpStatusCode.OK);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsPullRequestClient(
            httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        // Act
        var threads = await client.GetThreadsAsync(4711, "importer", TestContext.Current.CancellationToken);

        // Assert
        var thread = threads.ShouldHaveSingleItem();
        thread.Comments.Count.ShouldBe(1);
        thread.Comments[0].Id.ShouldBe(1);
    }

    [Fact]
    public async Task GetThreadsAsync_SortsThreadsNewestFirstAndCommentsAscending()
    {
        // Arrange
        var responseJson = """
        {
            "value": [
                {
                    "id": 1,
                    "status": "active",
                    "publishedDate": "2026-05-13T09:00:00Z",
                    "comments": [
                        { "id": 11, "content": "later", "author": { "displayName": "u" }, "publishedDate": "2026-05-13T09:05:00Z" },
                        { "id": 10, "content": "earlier", "author": { "displayName": "u" }, "publishedDate": "2026-05-13T09:00:00Z" }
                    ]
                },
                {
                    "id": 2,
                    "status": "active",
                    "publishedDate": "2026-05-13T11:00:00Z",
                    "comments": [
                        { "id": 20, "content": "x", "author": { "displayName": "u" }, "publishedDate": "2026-05-13T11:00:00Z" }
                    ]
                }
            ]
        }
        """;
        using var handler = new FakeHttpHandler(responseJson, HttpStatusCode.OK);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsPullRequestClient(
            httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        // Act
        var threads = await client.GetThreadsAsync(4711, "importer", TestContext.Current.CancellationToken);

        // Assert
        threads.Select(t => t.Id).ShouldBe([2, 1]);
        threads[1].Comments.Select(c => c.Id).ShouldBe([10, 11]);
    }

    [Fact]
    public async Task GetThreadsAsync_UnresolvedAuthor_AuthorIsNull()
    {
        // Arrange
        var responseJson = """
        {
            "value": [
                {
                    "id": 1,
                    "status": "active",
                    "publishedDate": "2026-05-13T09:00:00Z",
                    "comments": [
                        { "id": 1, "content": "x", "publishedDate": "2026-05-13T09:00:00Z" }
                    ]
                }
            ]
        }
        """;
        using var handler = new FakeHttpHandler(responseJson, HttpStatusCode.OK);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsPullRequestClient(
            httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        // Act
        var threads = await client.GetThreadsAsync(4711, "importer", TestContext.Current.CancellationToken);

        // Assert
        threads.ShouldHaveSingleItem().Comments[0].Author.ShouldBeNull();
    }

    [Fact]
    public async Task GetThreadsAsync_Non2xx_ThrowsHttpRequestException()
    {
        // Arrange
        using var handler = new FakeHttpHandler("""{"message":"nope"}""", HttpStatusCode.NotFound);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsPullRequestClient(
            httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        // Act
        Func<Task> act = () => client.GetThreadsAsync(4711, "importer", TestContext.Current.CancellationToken);

        // Assert
        await Should.ThrowAsync<HttpRequestException>(act);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetThreadsAsync_InvalidId_ThrowsArgumentOutOfRangeException(int id)
    {
        // Arrange
        using var handler = new FakeHttpHandler("{}", HttpStatusCode.OK);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsPullRequestClient(
            httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        // Act
        Func<Task> act = () => client.GetThreadsAsync(id, "importer", TestContext.Current.CancellationToken);

        // Assert
        await Should.ThrowAsync<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public async Task GetThreadsAsync_EmptyRepo_ThrowsArgumentException()
    {
        // Arrange
        using var handler = new FakeHttpHandler("{}", HttpStatusCode.OK);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsPullRequestClient(
            httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        // Act
        Func<Task> act = () => client.GetThreadsAsync(4711, string.Empty, TestContext.Current.CancellationToken);

        // Assert
        await Should.ThrowAsync<ArgumentException>(act);
    }
}
