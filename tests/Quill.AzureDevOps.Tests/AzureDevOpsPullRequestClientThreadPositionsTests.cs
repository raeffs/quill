using System.Net;
using Quill.Tests.Shared;
using Shouldly;

namespace Quill.AzureDevOps.Tests;

public class AzureDevOpsPullRequestClientThreadPositionsTests
{
    private const string ThreadsPrefix =
        $"{TestConstants.ServerUrl}/{TestConstants.Collection}/{TestConstants.Project}/_apis/git/repositories/importer/pullRequests/4711/threads?";

    [Fact]
    public async Task GetThreadsAsync_ScopesThreadsToHighestIterationId()
    {
        // Arrange - the highest id is not the last element by position.
        using var handler = new RecordingHandler(
        [
            new FakeResponse(HttpStatusCode.OK, """{"value":[{"id":1},{"id":3},{"id":2}]}"""),
            new FakeResponse(HttpStatusCode.OK, """{"value":[]}"""),
        ]);
        using var httpClient = NewHttpClient(handler);
        var client = NewClient(httpClient);

        // Act
        await client.GetThreadsAsync(4711, "importer", TestContext.Current.CancellationToken);

        // Assert
        handler.Requests.Count.ShouldBe(2);
        handler.Requests[0].Url!.ShouldContain("/pullRequests/4711/iterations?");
        handler.Requests[1].Url!.ShouldStartWith(ThreadsPrefix);
        handler.Requests[1].Url!.ShouldContain("$iteration=3");
        handler.Requests[1].Url!.ShouldContain("$baseIteration=0");
    }

    [Fact]
    public async Task GetThreadsAsync_SingleIteration_ScopesToThatIteration()
    {
        // Arrange
        using var handler = new RecordingHandler(
        [
            new FakeResponse(HttpStatusCode.OK, """{"value":[{"id":1}]}"""),
            new FakeResponse(HttpStatusCode.OK, """{"value":[]}"""),
        ]);
        using var httpClient = NewHttpClient(handler);
        var client = NewClient(httpClient);

        // Act
        await client.GetThreadsAsync(4711, "importer", TestContext.Current.CancellationToken);

        // Assert
        handler.Requests[1].Url!.ShouldContain("$iteration=1");
        handler.Requests[1].Url!.ShouldContain("$baseIteration=0");
    }

    [Fact]
    public async Task GetThreadsAsync_NoIterations_AsksForThreadsUnscoped()
    {
        // Arrange
        using var handler = new RecordingHandler(
        [
            new FakeResponse(HttpStatusCode.OK, """{"value":[]}"""),
            new FakeResponse(HttpStatusCode.OK, Threads("""
            {
                "id": 1,
                "status": "active",
                "publishedDate": "2026-05-13T09:00:00Z",
                "comments": [
                    { "id": 1, "content": "kept", "publishedDate": "2026-05-13T09:00:00Z" }
                ]
            }
            """)),
        ]);
        using var httpClient = NewHttpClient(handler);
        var client = NewClient(httpClient);

        // Act
        var threads = await client.GetThreadsAsync(4711, "importer", TestContext.Current.CancellationToken);

        // Assert
        threads.ShouldHaveSingleItem().Id.ShouldBe(1);
        handler.Requests[1].Url!.ShouldStartWith(ThreadsPrefix);
        handler.Requests[1].Url!.ShouldNotContain("$iteration");
        handler.Requests[1].Url!.ShouldNotContain("$baseIteration");
    }

    [Fact]
    public async Task GetThreadsAsync_CommentOnLatestIteration_PositionStateIsCurrent()
    {
        // Arrange
        using var handler = ScopedHandler(Threads("""
        {
            "id": 10,
            "status": "active",
            "publishedDate": "2026-05-13T09:00:00Z",
            "threadContext": {
                "filePath": "/src/Importer/Retry.cs",
                "rightFileStart": { "line": 42, "offset": 5 },
                "rightFileEnd": { "line": 42, "offset": 18 }
            },
            "pullRequestThreadContext": {
                "iterationContext": { "firstComparingIteration": 3, "secondComparingIteration": 3 }
            },
            "comments": [
                { "id": 1, "content": "Consider backoff.", "publishedDate": "2026-05-13T09:00:00Z" }
            ]
        }
        """));
        using var httpClient = NewHttpClient(handler);
        var client = NewClient(httpClient);

        // Act
        var thread = (await client.GetThreadsAsync(4711, "importer", TestContext.Current.CancellationToken))
            .ShouldHaveSingleItem();

        // Assert
        thread.PositionState.ShouldBe("current");
        thread.StartLine.ShouldBe(42);
        thread.EndLine.ShouldBe(42);
        thread.OrigStartLine.ShouldBe(42);
        thread.OrigEndLine.ShouldBe(42);
        thread.OrigStartColumn.ShouldBe(5);
        thread.OrigEndColumn.ShouldBe(18);
    }

    [Fact]
    public async Task GetThreadsAsync_TrackedThread_ReportsBothPositions()
    {
        // Arrange - the commented code moved from line 40 to line 57.
        using var handler = ScopedHandler(Threads("""
        {
            "id": 11,
            "status": "active",
            "publishedDate": "2026-05-13T09:00:00Z",
            "threadContext": {
                "filePath": "/src/Importer/Retry.cs",
                "rightFileStart": { "line": 57, "offset": 1 },
                "rightFileEnd": { "line": 57, "offset": 2147483647 }
            },
            "pullRequestThreadContext": {
                "iterationContext": { "firstComparingIteration": 1, "secondComparingIteration": 1 },
                "trackingCriteria": {
                    "origFilePath": "/src/Importer/Retry.cs",
                    "origRightFileStart": { "line": 40, "offset": 1 },
                    "origRightFileEnd": { "line": 40, "offset": 2147483647 }
                }
            },
            "comments": [
                { "id": 1, "content": "Consider backoff.", "publishedDate": "2026-05-13T09:00:00Z" }
            ]
        }
        """));
        using var httpClient = NewHttpClient(handler);
        var client = NewClient(httpClient);

        // Act
        var thread = (await client.GetThreadsAsync(4711, "importer", TestContext.Current.CancellationToken))
            .ShouldHaveSingleItem();

        // Assert
        thread.PositionState.ShouldBe("tracked");
        thread.FilePath.ShouldBe("src/Importer/Retry.cs");
        thread.StartLine.ShouldBe(57);
        thread.EndLine.ShouldBe(57);
        thread.OrigStartLine.ShouldBe(40);
        thread.OrigEndLine.ShouldBe(40);
        thread.OrigStartColumn.ShouldBe(1);
        thread.OrigEndColumn.ShouldBeNull();
        thread.OrigFilePath.ShouldBeNull();
    }

    [Fact]
    public async Task GetThreadsAsync_TrackedThreadThatDidNotMove_ReportsTheSameLine()
    {
        // Arrange
        using var handler = ScopedHandler(Threads("""
        {
            "id": 12,
            "status": "active",
            "publishedDate": "2026-05-13T09:00:00Z",
            "threadContext": {
                "filePath": "/src/Importer/Retry.cs",
                "rightFileStart": { "line": 40, "offset": 1 },
                "rightFileEnd": { "line": 40, "offset": 2147483647 }
            },
            "pullRequestThreadContext": {
                "trackingCriteria": {
                    "origRightFileStart": { "line": 40, "offset": 1 },
                    "origRightFileEnd": { "line": 40, "offset": 2147483647 }
                }
            },
            "comments": [
                { "id": 1, "content": "x", "publishedDate": "2026-05-13T09:00:00Z" }
            ]
        }
        """));
        using var httpClient = NewHttpClient(handler);
        var client = NewClient(httpClient);

        // Act
        var thread = (await client.GetThreadsAsync(4711, "importer", TestContext.Current.CancellationToken))
            .ShouldHaveSingleItem();

        // Assert
        thread.PositionState.ShouldBe("tracked");
        thread.StartLine.ShouldBe(40);
        thread.OrigStartLine.ShouldBe(40);
    }

    [Fact]
    public async Task GetThreadsAsync_ZeroWidthCaret_PositionStateIsDeleted()
    {
        // Arrange
        using var handler = ScopedHandler(Threads("""
        {
            "id": 13,
            "status": "active",
            "publishedDate": "2026-05-13T09:00:00Z",
            "threadContext": {
                "filePath": "/src/Importer/Retry.cs",
                "rightFileStart": { "line": 31, "offset": 1 },
                "rightFileEnd": { "line": 31, "offset": 1 }
            },
            "pullRequestThreadContext": {
                "trackingCriteria": {
                    "origRightFileStart": { "line": 88, "offset": 1 },
                    "origRightFileEnd": { "line": 88, "offset": 1 }
                }
            },
            "comments": [
                { "id": 1, "content": "This is dead code.", "publishedDate": "2026-05-13T09:00:00Z" }
            ]
        }
        """));
        using var httpClient = NewHttpClient(handler);
        var client = NewClient(httpClient);

        // Act
        var thread = (await client.GetThreadsAsync(4711, "importer", TestContext.Current.CancellationToken))
            .ShouldHaveSingleItem();

        // Assert
        thread.PositionState.ShouldBe("deleted");
        thread.StartLine.ShouldBe(31);
        thread.OrigStartLine.ShouldBe(88);
        thread.OrigStartColumn.ShouldBe(1);
        thread.OrigEndColumn.ShouldBe(1);
    }

    [Fact]
    public async Task GetThreadsAsync_NoTrackingCriteria_PositionStateIsUnverified()
    {
        // Arrange
        using var handler = ScopedHandler(Threads("""
        {
            "id": 14,
            "status": "active",
            "publishedDate": "2026-05-13T09:00:00Z",
            "threadContext": {
                "filePath": "/src/Importer/Retry.cs",
                "rightFileStart": { "line": 13, "offset": 3 },
                "rightFileEnd": { "line": 13, "offset": 20 }
            },
            "pullRequestThreadContext": {
                "iterationContext": { "firstComparingIteration": 1, "secondComparingIteration": 1 }
            },
            "comments": [
                { "id": 1, "content": "x", "publishedDate": "2026-05-13T09:00:00Z" }
            ]
        }
        """));
        using var httpClient = NewHttpClient(handler);
        var client = NewClient(httpClient);

        // Act
        var thread = (await client.GetThreadsAsync(4711, "importer", TestContext.Current.CancellationToken))
            .ShouldHaveSingleItem();

        // Assert
        thread.PositionState.ShouldBe("unverified");
        thread.StartLine.ShouldBe(13);
        thread.OrigStartLine.ShouldBe(13);
        thread.OrigEndLine.ShouldBe(13);
        thread.OrigStartColumn.ShouldBe(3);
        thread.OrigEndColumn.ShouldBe(20);
    }

    [Fact]
    public async Task GetThreadsAsync_ThreadWithoutThreadContext_HasNoPositionState()
    {
        // Arrange - no anchor at all: a comment on the pull request.
        using var handler = ScopedHandler(Threads("""
        {
            "id": 15,
            "status": "active",
            "publishedDate": "2026-05-13T09:00:00Z",
            "comments": [
                { "id": 1, "content": "Overall, LGTM.", "publishedDate": "2026-05-13T09:00:00Z" }
            ]
        }
        """));
        using var httpClient = NewHttpClient(handler);
        var client = NewClient(httpClient);

        // Act
        var thread = (await client.GetThreadsAsync(4711, "importer", TestContext.Current.CancellationToken))
            .ShouldHaveSingleItem();

        // Assert
        thread.FilePath.ShouldBeNull();
        thread.PositionState.ShouldBeNull();
        thread.OrigStartLine.ShouldBeNull();
        thread.OrigEndLine.ShouldBeNull();
        thread.OrigStartColumn.ShouldBeNull();
        thread.OrigEndColumn.ShouldBeNull();
        thread.OrigFilePath.ShouldBeNull();
    }

    [Fact]
    public async Task GetThreadsAsync_FileWithoutLineRange_KeepsTheFile()
    {
        // Arrange
        using var handler = ScopedHandler(Threads("""
        {
            "id": 16,
            "status": "active",
            "publishedDate": "2026-05-13T09:00:00Z",
            "threadContext": { "filePath": "/src/Importer/Retry.cs" },
            "comments": [
                { "id": 1, "content": "This whole file needs a rewrite.", "publishedDate": "2026-05-13T09:00:00Z" }
            ]
        }
        """));
        using var httpClient = NewHttpClient(handler);
        var client = NewClient(httpClient);

        // Act
        var thread = (await client.GetThreadsAsync(4711, "importer", TestContext.Current.CancellationToken))
            .ShouldHaveSingleItem();

        // Assert
        thread.FilePath.ShouldBe("src/Importer/Retry.cs");
        thread.Side.ShouldBeNull();
        thread.StartLine.ShouldBeNull();
        thread.OrigStartLine.ShouldBeNull();
        thread.PositionState.ShouldBe("unverified");
    }

    [Fact]
    public async Task GetThreadsAsync_RenamedFile_EmitsOrigFilePath()
    {
        // Arrange
        using var handler = ScopedHandler(Threads("""
        {
            "id": 17,
            "status": "active",
            "publishedDate": "2026-05-13T09:00:00Z",
            "threadContext": {
                "filePath": "/src/Importer/RetryPolicy.cs",
                "rightFileStart": { "line": 12, "offset": 1 },
                "rightFileEnd": { "line": 12, "offset": 2147483647 }
            },
            "pullRequestThreadContext": {
                "trackingCriteria": {
                    "origFilePath": "/src/Importer/Retry.cs",
                    "origRightFileStart": { "line": 9, "offset": 1 },
                    "origRightFileEnd": { "line": 9, "offset": 2147483647 }
                }
            },
            "comments": [
                { "id": 1, "content": "x", "publishedDate": "2026-05-13T09:00:00Z" }
            ]
        }
        """));
        using var httpClient = NewHttpClient(handler);
        var client = NewClient(httpClient);

        // Act
        var thread = (await client.GetThreadsAsync(4711, "importer", TestContext.Current.CancellationToken))
            .ShouldHaveSingleItem();

        // Assert
        thread.FilePath.ShouldBe("src/Importer/RetryPolicy.cs");
        thread.OrigFilePath.ShouldBe("src/Importer/Retry.cs");
    }

    [Fact]
    public async Task GetThreadsAsync_LeftSideTrackedThread_ReadsLeftOriginalPositions()
    {
        // Arrange
        using var handler = ScopedHandler(Threads("""
        {
            "id": 18,
            "status": "active",
            "publishedDate": "2026-05-13T09:00:00Z",
            "threadContext": {
                "filePath": "/src/Importer/Old.cs",
                "leftFileStart": { "line": 21, "offset": 1 },
                "leftFileEnd": { "line": 24, "offset": 2147483647 }
            },
            "pullRequestThreadContext": {
                "trackingCriteria": {
                    "origLeftFileStart": { "line": 18, "offset": 1 },
                    "origLeftFileEnd": { "line": 22, "offset": 2147483647 }
                }
            },
            "comments": [
                { "id": 1, "content": "x", "publishedDate": "2026-05-13T09:00:00Z" }
            ]
        }
        """));
        using var httpClient = NewHttpClient(handler);
        var client = NewClient(httpClient);

        // Act
        var thread = (await client.GetThreadsAsync(4711, "importer", TestContext.Current.CancellationToken))
            .ShouldHaveSingleItem();

        // Assert
        thread.Side.ShouldBe("left");
        thread.StartLine.ShouldBe(21);
        thread.EndLine.ShouldBe(24);
        thread.OrigStartLine.ShouldBe(18);
        thread.OrigEndLine.ShouldBe(22);
        thread.OrigEndColumn.ShouldBeNull();
        thread.PositionState.ShouldBe("tracked");
    }

    [Fact]
    public async Task GetThreadsAsync_MapsThreadDates()
    {
        // Arrange - a resolution that added no comment moves the thread date past its newest comment.
        using var handler = ScopedHandler(Threads("""
        {
            "id": 19,
            "status": "fixed",
            "publishedDate": "2026-05-13T09:00:00Z",
            "lastUpdatedDate": "2026-05-14T16:30:00Z",
            "comments": [
                { "id": 1, "content": "x", "publishedDate": "2026-05-13T09:00:00Z" }
            ]
        }
        """));
        using var httpClient = NewHttpClient(handler);
        var client = NewClient(httpClient);

        // Act
        var thread = (await client.GetThreadsAsync(4711, "importer", TestContext.Current.CancellationToken))
            .ShouldHaveSingleItem();

        // Assert
        thread.PublishedDate.ShouldBe(new DateTimeOffset(2026, 5, 13, 9, 0, 0, TimeSpan.Zero));
        thread.LastUpdatedDate.ShouldBe(new DateTimeOffset(2026, 5, 14, 16, 30, 0, TimeSpan.Zero));
        thread.LastUpdatedDate.ShouldBeGreaterThan(thread.Comments[0].LastUpdatedDate);
    }

    [Fact]
    public async Task GetThreadsAsync_MapsCommentDatesAndLikes()
    {
        // Arrange
        using var handler = ScopedHandler(Threads("""
        {
            "id": 20,
            "status": "active",
            "publishedDate": "2026-05-13T09:00:00Z",
            "comments": [
                {
                    "id": 1,
                    "content": "x",
                    "publishedDate": "2026-05-13T09:00:00Z",
                    "lastContentUpdatedDate": "2026-05-13T09:40:00Z",
                    "lastUpdatedDate": "2026-05-13T11:00:00Z",
                    "usersLiked": [ { "displayName": "Jane Doe" }, { "displayName": "John Roe" } ]
                }
            ]
        }
        """));
        using var httpClient = NewHttpClient(handler);
        var client = NewClient(httpClient);

        // Act
        var comment = (await client.GetThreadsAsync(4711, "importer", TestContext.Current.CancellationToken))
            .ShouldHaveSingleItem().Comments.ShouldHaveSingleItem();

        // Assert
        comment.CreatedDate.ShouldBe(new DateTimeOffset(2026, 5, 13, 9, 0, 0, TimeSpan.Zero));
        comment.ModifiedDate.ShouldBe(new DateTimeOffset(2026, 5, 13, 9, 40, 0, TimeSpan.Zero));
        comment.LastUpdatedDate.ShouldBe(new DateTimeOffset(2026, 5, 13, 11, 0, 0, TimeSpan.Zero));
        comment.UsersLiked.ShouldBe(["Jane Doe", "John Roe"]);
    }

    [Fact]
    public async Task GetThreadsAsync_CommentWithoutLikes_HasEmptyUsersLiked()
    {
        // Arrange
        using var handler = ScopedHandler(Threads("""
        {
            "id": 21,
            "status": "active",
            "publishedDate": "2026-05-13T09:00:00Z",
            "comments": [
                { "id": 1, "content": "x", "publishedDate": "2026-05-13T09:00:00Z", "usersLiked": [] }
            ]
        }
        """));
        using var httpClient = NewHttpClient(handler);
        var client = NewClient(httpClient);

        // Act
        var comment = (await client.GetThreadsAsync(4711, "importer", TestContext.Current.CancellationToken))
            .ShouldHaveSingleItem().Comments.ShouldHaveSingleItem();

        // Assert
        comment.UsersLiked.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetThreadsAsync_CommentWithoutLastUpdatedDate_FallsBackToPublishedDate()
    {
        // Arrange
        using var handler = ScopedHandler(Threads("""
        {
            "id": 22,
            "status": "active",
            "publishedDate": "2026-05-13T09:00:00Z",
            "comments": [
                { "id": 1, "content": "x", "publishedDate": "2026-05-13T09:00:00Z" }
            ]
        }
        """));
        using var httpClient = NewHttpClient(handler);
        var client = NewClient(httpClient);

        // Act
        var comment = (await client.GetThreadsAsync(4711, "importer", TestContext.Current.CancellationToken))
            .ShouldHaveSingleItem().Comments.ShouldHaveSingleItem();

        // Assert
        comment.LastUpdatedDate.ShouldBe(new DateTimeOffset(2026, 5, 13, 9, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public async Task GetThreadsAsync_ThreadWithoutLastUpdatedDate_FallsBackToPublishedDate()
    {
        // Arrange
        using var handler = ScopedHandler(Threads("""
        {
            "id": 23,
            "status": "active",
            "publishedDate": "2026-05-13T09:00:00Z",
            "comments": [
                { "id": 1, "content": "x", "publishedDate": "2026-05-13T09:00:00Z" }
            ]
        }
        """));
        using var httpClient = NewHttpClient(handler);
        var client = NewClient(httpClient);

        // Act
        var thread = (await client.GetThreadsAsync(4711, "importer", TestContext.Current.CancellationToken))
            .ShouldHaveSingleItem();

        // Assert
        thread.LastUpdatedDate.ShouldBe(thread.PublishedDate);
    }

    [Fact]
    public async Task GetThreadsAsync_NullContentComment_IsFiltered()
    {
        // Arrange
        using var handler = ScopedHandler(Threads("""
        {
            "id": 24,
            "status": "active",
            "publishedDate": "2026-05-13T09:00:00Z",
            "comments": [
                { "id": 1, "content": null, "publishedDate": "2026-05-13T09:00:00Z" },
                { "id": 2, "content": "kept", "publishedDate": "2026-05-13T09:05:00Z" }
            ]
        }
        """));
        using var httpClient = NewHttpClient(handler);
        var client = NewClient(httpClient);

        // Act
        var thread = (await client.GetThreadsAsync(4711, "importer", TestContext.Current.CancellationToken))
            .ShouldHaveSingleItem();

        // Assert
        thread.Comments.ShouldHaveSingleItem().Id.ShouldBe(2);
    }

    [Fact]
    public async Task GetThreadsAsync_ThreadWithOnlyNullContentComments_DropsOut()
    {
        // Arrange
        using var handler = ScopedHandler(Threads("""
        {
            "id": 25,
            "status": "active",
            "publishedDate": "2026-05-13T09:00:00Z",
            "comments": [
                { "id": 1, "content": null, "publishedDate": "2026-05-13T09:00:00Z" }
            ]
        }
        """));
        using var httpClient = NewHttpClient(handler);
        var client = NewClient(httpClient);

        // Act
        var threads = await client.GetThreadsAsync(4711, "importer", TestContext.Current.CancellationToken);

        // Assert
        threads.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetThreadsAsync_ThreadWithoutCommentsKey_DropsOut()
    {
        // Arrange - the source-generated context leaves an absent array null, not empty.
        using var handler = ScopedHandler(Threads("""
        {
            "id": 26,
            "status": "active",
            "publishedDate": "2026-05-13T09:00:00Z"
        }
        """));
        using var httpClient = NewHttpClient(handler);
        var client = NewClient(httpClient);

        // Act
        var threads = await client.GetThreadsAsync(4711, "importer", TestContext.Current.CancellationToken);

        // Assert
        threads.ShouldBeEmpty();
    }

    private static string Threads(string threadObject) =>
        $$"""{"value":[{{threadObject}}]}""";

    private static RecordingHandler ScopedHandler(string threadsJson) => new(
    [
        new FakeResponse(HttpStatusCode.OK, """{"value":[{"id":1},{"id":2},{"id":3}]}"""),
        new FakeResponse(HttpStatusCode.OK, threadsJson),
    ]);

    private static HttpClient NewHttpClient(RecordingHandler handler) =>
        new(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };

    private static AzureDevOpsPullRequestClient NewClient(HttpClient httpClient) => new(
        httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);
}
