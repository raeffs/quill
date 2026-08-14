using System.Net;
using Quill.Tests.Shared;
using Shouldly;

namespace Quill.AzureDevOps.Tests;

public class AzureDevOpsClientCommentsTests
{
    [Fact]
    public async Task GetCommentsAsync_ValidResponse_MapsFieldsAndOrderDesc()
    {
        // API is queried with order=desc, so the response arrives newest-first and we preserve that order.
        var rawApiJson = """
        {
            "totalCount": 2,
            "count": 2,
            "comments": [
                {
                    "id": 9002,
                    "text": "<p>Second</p>",
                    "createdBy": { "displayName": "John Roe" },
                    "createdDate": "2026-04-11T08:00:00Z"
                },
                {
                    "id": 9001,
                    "text": "<p>First</p>",
                    "createdBy": { "displayName": "Jane Doe" },
                    "createdDate": "2026-04-10T12:34:56Z",
                    "modifiedDate": "2026-04-10T13:02:11Z"
                }
            ]
        }
        """;

        using var handler = new FakeHttpHandler(rawApiJson, HttpStatusCode.OK);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsClient(httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        var comments = await client.GetCommentsAsync(123, cancellationToken: TestContext.Current.CancellationToken);

        comments.Count.ShouldBe(2);
        comments[0].Id.ShouldBe(9002);
        comments[0].Author.ShouldBe("John Roe");
        comments[0].ModifiedDate.ShouldBeNull();
        comments[0].TextHtml.ShouldBe("<p>Second</p>");
        comments[1].Id.ShouldBe(9001);
        comments[1].ModifiedDate.ShouldNotBeNull();

        handler.LastRequest.ShouldNotBeNull();
        var requestUri = handler.LastRequest.RequestUri!.ToString();
        requestUri.ShouldContain("/workitems/123/comments");
        requestUri.ShouldContain("order=desc");
    }

    [Fact]
    public async Task GetCommentsAsync_DeletedComment_IsFiltered()
    {
        var rawApiJson = """
        {
            "totalCount": 2,
            "count": 2,
            "comments": [
                { "id": 2, "text": "<p>kept</p>", "createdBy": { "displayName": "J" }, "createdDate": "2026-04-11T08:00:00Z", "isDeleted": true },
                { "id": 1, "text": "<p>also kept</p>", "createdBy": { "displayName": "J" }, "createdDate": "2026-04-10T08:00:00Z" }
            ]
        }
        """;

        using var handler = new FakeHttpHandler(rawApiJson, HttpStatusCode.OK);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsClient(httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        var comments = await client.GetCommentsAsync(123, cancellationToken: TestContext.Current.CancellationToken);

        comments.ShouldHaveSingleItem().Id.ShouldBe(1);
    }

    [Fact]
    public async Task GetCommentsAsync_EmptyThread_ReturnsEmptyList()
    {
        var rawApiJson = """
        { "totalCount": 0, "count": 0, "comments": [] }
        """;

        using var handler = new FakeHttpHandler(rawApiJson, HttpStatusCode.OK);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsClient(httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        var comments = await client.GetCommentsAsync(123, cancellationToken: TestContext.Current.CancellationToken);

        comments.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetCommentsAsync_UnresolvedCreatedBy_AuthorIsNull()
    {
        var rawApiJson = """
        {
            "totalCount": 1,
            "count": 1,
            "comments": [
                { "id": 1, "text": "<p>x</p>", "createdDate": "2026-04-11T08:00:00Z" }
            ]
        }
        """;

        using var handler = new FakeHttpHandler(rawApiJson, HttpStatusCode.OK);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsClient(httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        var comments = await client.GetCommentsAsync(123, cancellationToken: TestContext.Current.CancellationToken);

        comments.ShouldHaveSingleItem().Author.ShouldBeNull();
    }

    [Fact]
    public async Task GetCommentsAsync_LimitSmallerThanPage_TruncatesAndSkipsPagination()
    {
        var rawApiJson = """
        {
            "totalCount": 5,
            "count": 3,
            "comments": [
                { "id": 5, "text": "<p>e</p>", "createdBy": { "displayName": "u" }, "createdDate": "2026-04-11T05:00:00Z" },
                { "id": 4, "text": "<p>d</p>", "createdBy": { "displayName": "u" }, "createdDate": "2026-04-11T04:00:00Z" },
                { "id": 3, "text": "<p>c</p>", "createdBy": { "displayName": "u" }, "createdDate": "2026-04-11T03:00:00Z" }
            ],
            "continuationToken": "tok"
        }
        """;

        using var handler = new FakeHttpHandler(rawApiJson, HttpStatusCode.OK);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsClient(httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        var comments = await client.GetCommentsAsync(123, limit: 2, cancellationToken: TestContext.Current.CancellationToken);

        comments.Count.ShouldBe(2);
        comments[0].Id.ShouldBe(5);
        comments[1].Id.ShouldBe(4);
        handler.LastRequest!.RequestUri!.ToString().ShouldContain("$top=2");
    }

    [Fact]
    public async Task GetCommentsAsync_MultiplePages_FollowsContinuationTokenUntilExhausted()
    {
        var page1 = """
        {
            "totalCount": 4,
            "count": 2,
            "comments": [
                { "id": 4, "text": "<p>d</p>", "createdBy": { "displayName": "u" }, "createdDate": "2026-04-11T04:00:00Z" },
                { "id": 3, "text": "<p>c</p>", "createdBy": { "displayName": "u" }, "createdDate": "2026-04-11T03:00:00Z" }
            ],
            "continuationToken": "next-page"
        }
        """;

        var page2 = """
        {
            "totalCount": 4,
            "count": 2,
            "comments": [
                { "id": 2, "text": "<p>b</p>", "createdBy": { "displayName": "u" }, "createdDate": "2026-04-11T02:00:00Z" },
                { "id": 1, "text": "<p>a</p>", "createdBy": { "displayName": "u" }, "createdDate": "2026-04-11T01:00:00Z" }
            ]
        }
        """;

        using var handler = new ScriptedHttpHandler([page1, page2]);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsClient(httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        var comments = await client.GetCommentsAsync(123, cancellationToken: TestContext.Current.CancellationToken);

        comments.Select(c => c.Id).ShouldBe([4, 3, 2, 1]);
        handler.Requests.Count.ShouldBe(2);
        handler.Requests[1].RequestUri!.ToString().ShouldContain("continuationToken=next-page");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetCommentsAsync_InvalidId_ThrowsArgumentOutOfRangeException(int id)
    {
        using var handler = new FakeHttpHandler("{}", HttpStatusCode.OK);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsClient(httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        Func<Task> act = () => client.GetCommentsAsync(id);

        await Should.ThrowAsync<ArgumentOutOfRangeException>(act);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetCommentsAsync_InvalidLimit_ThrowsArgumentOutOfRangeException(int limit)
    {
        using var handler = new FakeHttpHandler("{}", HttpStatusCode.OK);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsClient(httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        Func<Task> act = () => client.GetCommentsAsync(1, limit);

        await Should.ThrowAsync<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public async Task GetCommentsAsync_Non2xx_ThrowsHttpRequestException()
    {
        using var handler = new FakeHttpHandler("""{"message":"nope"}""", HttpStatusCode.NotFound);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsClient(httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        Func<Task> act = () => client.GetCommentsAsync(999);

        await Should.ThrowAsync<HttpRequestException>(act);
    }
}

internal class ScriptedHttpHandler : HttpMessageHandler
{
    private readonly Queue<string> _responses;

    public ScriptedHttpHandler(IEnumerable<string> responses)
    {
        _responses = new Queue<string>(responses);
    }

    public List<HttpRequestMessage> Requests { get; } = new();

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        var body = _responses.Count > 0 ? _responses.Dequeue() : "{}";
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json"),
        });
    }
}
