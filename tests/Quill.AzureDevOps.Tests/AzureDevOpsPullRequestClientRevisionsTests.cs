using System.Globalization;
using System.Net;
using Quill.Tests.Shared;
using Shouldly;

namespace Quill.AzureDevOps.Tests;

public class AzureDevOpsPullRequestClientRevisionsTests
{
    [Fact]
    public async Task GetRevisionsAsync_HitsRepoScopedIterationsEndpoint()
    {
        // Arrange
        using var handler = new RecordingHandler([new FakeResponse(HttpStatusCode.OK, """{"value":[]}""")]);
        using var httpClient = NewHttpClient(handler);
        var client = NewClient(httpClient);

        // Act
        var revisions = await client.GetRevisionsAsync(4711, "importer", TestContext.Current.CancellationToken);

        // Assert
        revisions.ShouldBeEmpty();
        handler.Requests[0].Url!.ShouldStartWith(
            $"{TestConstants.ServerUrl}/{TestConstants.Collection}/{TestConstants.Project}/_apis/git/repositories/importer/pullRequests/4711/iterations?");
    }

    [Fact]
    public async Task GetRevisionsAsync_MapsEveryKeyOfTheRow()
    {
        // Arrange
        using var handler = IterationsHandler("""
        {
            "value": [
                {
                    "id": 1,
                    "description": "Shared formatter",
                    "author": { "displayName": "Jane Doe" },
                    "createdDate": "2026-08-14T08:20:54.8691025Z",
                    "sourceRefCommit": { "commitId": "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa" },
                    "targetRefCommit": { "commitId": "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb" },
                    "commonRefCommit": { "commitId": "cccccccccccccccccccccccccccccccccccccccc" },
                    "reason": "push"
                }
            ]
        }
        """);
        using var httpClient = NewHttpClient(handler);
        var client = NewClient(httpClient);

        // Act
        var revisions = await client.GetRevisionsAsync(4711, "importer", TestContext.Current.CancellationToken);

        // Assert
        var revision = revisions.ShouldHaveSingleItem();
        revision.Id.ShouldBe(1);
        revision.Author.ShouldBe("Jane Doe");
        revision.CreatedDate.ShouldBe(DateTimeOffset.Parse("2026-08-14T08:20:54.8691025Z", CultureInfo.InvariantCulture));
        revision.SourceCommit.ShouldBe("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");
        revision.TargetCommit.ShouldBe("bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb");
        revision.CommonCommit.ShouldBe("cccccccccccccccccccccccccccccccccccccccc");
    }

    [Fact]
    public async Task GetRevisionsAsync_SortsNewestFirst()
    {
        // Arrange
        using var handler = IterationsHandler("""
        {
            "value": [
                { "id": 1, "createdDate": "2026-08-14T08:00:00Z" },
                { "id": 3, "createdDate": "2026-08-14T10:00:00Z" },
                { "id": 2, "createdDate": "2026-08-14T09:00:00Z" }
            ]
        }
        """);
        using var httpClient = NewHttpClient(handler);
        var client = NewClient(httpClient);

        // Act
        var revisions = await client.GetRevisionsAsync(4711, "importer", TestContext.Current.CancellationToken);

        // Assert
        revisions.Select(r => r.Id).ShouldBe([3, 2, 1]);
    }

    [Fact]
    public async Task GetRevisionsAsync_UnresolvedAuthor_AuthorIsNull()
    {
        // Arrange
        using var handler = IterationsHandler("""
        {
            "value": [
                { "id": 1, "createdDate": "2026-08-14T08:00:00Z" }
            ]
        }
        """);
        using var httpClient = NewHttpClient(handler);
        var client = NewClient(httpClient);

        // Act
        var revisions = await client.GetRevisionsAsync(4711, "importer", TestContext.Current.CancellationToken);

        // Assert
        revisions.ShouldHaveSingleItem().Author.ShouldBeNull();
    }

    [Fact]
    public async Task GetRevisionsAsync_MissingCommitRef_CommitIsNull()
    {
        // Arrange
        using var handler = IterationsHandler("""
        {
            "value": [
                { "id": 1, "createdDate": "2026-08-14T08:00:00Z", "commonRefCommit": { "commitId": "" } }
            ]
        }
        """);
        using var httpClient = NewHttpClient(handler);
        var client = NewClient(httpClient);

        // Act
        var revisions = await client.GetRevisionsAsync(4711, "importer", TestContext.Current.CancellationToken);

        // Assert
        var revision = revisions.ShouldHaveSingleItem();
        revision.SourceCommit.ShouldBeNull();
        revision.TargetCommit.ShouldBeNull();
        revision.CommonCommit.ShouldBeNull();
    }

    [Fact]
    public async Task GetRevisionsAsync_AfterGetThreadsAsync_ReusesTheCachedFetch()
    {
        // Arrange: GetThreadsAsync already reads /iterations, so the second read must not hit the wire.
        using var handler = new RecordingHandler(
        [
            new FakeResponse(HttpStatusCode.OK, """{"value":[{"id":1,"createdDate":"2026-08-14T08:00:00Z"}]}"""),
            new FakeResponse(HttpStatusCode.OK, """{"value":[]}"""),
        ]);
        using var httpClient = NewHttpClient(handler);
        var client = NewClient(httpClient);

        // Act
        await client.GetThreadsAsync(4711, "importer", TestContext.Current.CancellationToken);
        var revisions = await client.GetRevisionsAsync(4711, "importer", TestContext.Current.CancellationToken);

        // Assert
        revisions.ShouldHaveSingleItem().Id.ShouldBe(1);
        handler.Requests.Count.ShouldBe(2);
    }

    [Fact]
    public async Task GetRevisionsAsync_Non2xx_ThrowsHttpRequestException()
    {
        // Arrange
        using var handler = new RecordingHandler(
        [
            new FakeResponse(HttpStatusCode.NotFound, """{"message":"nope"}"""),
        ]);
        using var httpClient = NewHttpClient(handler);
        var client = NewClient(httpClient);

        // Act
        Func<Task> act = () => client.GetRevisionsAsync(4711, "importer", TestContext.Current.CancellationToken);

        // Assert
        await Should.ThrowAsync<HttpRequestException>(act);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetRevisionsAsync_InvalidId_ThrowsArgumentOutOfRangeException(int id)
    {
        // Arrange
        using var handler = new FakeHttpHandler("{}", HttpStatusCode.OK);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsPullRequestClient(
            httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        // Act
        Func<Task> act = () => client.GetRevisionsAsync(id, "importer", TestContext.Current.CancellationToken);

        // Assert
        await Should.ThrowAsync<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public async Task GetRevisionsAsync_EmptyRepo_ThrowsArgumentException()
    {
        // Arrange
        using var handler = new FakeHttpHandler("{}", HttpStatusCode.OK);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsPullRequestClient(
            httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        // Act
        Func<Task> act = () => client.GetRevisionsAsync(4711, string.Empty, TestContext.Current.CancellationToken);

        // Assert
        await Should.ThrowAsync<ArgumentException>(act);
    }

    private static RecordingHandler IterationsHandler(string iterationsJson) =>
        new([new FakeResponse(HttpStatusCode.OK, iterationsJson)]);

    private static HttpClient NewHttpClient(RecordingHandler handler) =>
        new(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };

    private static AzureDevOpsPullRequestClient NewClient(HttpClient httpClient) => new(
        httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);
}
