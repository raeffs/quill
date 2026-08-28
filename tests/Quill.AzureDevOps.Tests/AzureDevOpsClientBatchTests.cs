using System.Net;
using Quill.AzureDevOps;
using Quill.Tests.Shared;
using Shouldly;

namespace Quill.AzureDevOps.Tests;

public class AzureDevOpsClientBatchTests
{
    [Fact]
    public async Task FetchAsync_SingleBatch_ReturnsMappedWorkItemsAndChildIds()
    {
        var responseJson = $$"""
        {
          "count": 2,
          "value": [
            {
              "id": 10,
              "fields": {
                "System.WorkItemType": "Feature",
                "System.Title": "F-ten",
                "System.State": "Active"
              },
              "relations": [
                { "rel": "System.LinkTypes.Hierarchy-Forward", "url": "{{TestConstants.ServerUrl}}/{{TestConstants.Collection}}/_apis/wit/workitems/100" },
                { "rel": "System.LinkTypes.Hierarchy-Forward", "url": "{{TestConstants.ServerUrl}}/{{TestConstants.Collection}}/_apis/wit/workitems/101" }
              ]
            },
            {
              "id": 11,
              "fields": {
                "System.WorkItemType": "Feature",
                "System.Title": "F-eleven",
                "System.State": "New"
              }
            }
          ]
        }
        """;

        using var handler = new RecordingHandler([new FakeResponse(HttpStatusCode.OK, responseJson)]);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsClient(httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        var result = await client.FetchAsync([10, 11], TestContext.Current.CancellationToken);

        result.Items.Count.ShouldBe(2);
        result.BatchFailedIds.ShouldBeEmpty();

        var ten = result.Items.Single(i => i.Id == 10);
        ten.Title.ShouldBe("F-ten");
        ten.Type.ShouldBe("Feature");
        ten.State.ShouldBe("Active");
        ten.ChildIds.ShouldBe([100, 101]);

        handler.Requests.Count.ShouldBe(1);
        handler.Requests[0].Method.ShouldBe(HttpMethod.Post);
        handler.Requests[0].Url!.ShouldContain("/_apis/wit/workitemsbatch");
        handler.RequestBodies[0].ShouldContain("\"ids\"");
        handler.RequestBodies[0].ShouldContain("\"errorPolicy\":\"omit\"");
        handler.RequestBodies[0].ShouldContain("\"$expand\":\"relations\"");
        handler.RequestBodies[0].ShouldContain("10");
        handler.RequestBodies[0].ShouldContain("11");
    }

    [Fact]
    public async Task FetchAsync_ErrorPolicyOmit_MissingIdsAreNotInItemsAndNotBatchFailed()
    {
        // Requested 3 ids; server returns only 2 (id 11 is 403/404 on the server side).
        var responseJson = """
        {
          "count": 2,
          "value": [
            { "id": 10, "fields": { "System.WorkItemType": "Feature", "System.Title": "ten", "System.State": "New" } },
            { "id": 12, "fields": { "System.WorkItemType": "Feature", "System.Title": "twelve", "System.State": "New" } }
          ]
        }
        """;

        using var handler = new RecordingHandler([new FakeResponse(HttpStatusCode.OK, responseJson)]);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsClient(httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        var result = await client.FetchAsync([10, 11, 12], TestContext.Current.CancellationToken);

        result.Items.Select(i => i.Id).ShouldBe([10, 12], ignoreOrder: true);
        result.BatchFailedIds.ShouldBeEmpty();
    }

    [Fact]
    public async Task FetchAsync_MoreThan200Ids_SplitsIntoSequentialSubBatches()
    {
        var ids = Enumerable.Range(1, 250).ToArray();
        var firstResponse = BuildBatchResponse(Enumerable.Range(1, 200));
        var secondResponse = BuildBatchResponse(Enumerable.Range(201, 50));

        using var handler = new RecordingHandler(
        [
            new FakeResponse(HttpStatusCode.OK, firstResponse),
            new FakeResponse(HttpStatusCode.OK, secondResponse),
        ]);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsClient(httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        var result = await client.FetchAsync(ids, TestContext.Current.CancellationToken);

        result.Items.Count.ShouldBe(250);
        result.BatchFailedIds.ShouldBeEmpty();
        handler.Requests.Count.ShouldBe(2);
    }

    [Fact]
    public async Task FetchAsync_TransientFailureThenSuccess_RetriesOnceAndReturnsItems()
    {
        var responseJson = BuildBatchResponse([10, 11]);

        using var handler = new RecordingHandler(
        [
            new FakeResponse(HttpStatusCode.InternalServerError, "boom"),
            new FakeResponse(HttpStatusCode.OK, responseJson),
        ]);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsClient(httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        var result = await client.FetchAsync([10, 11], TestContext.Current.CancellationToken);

        result.Items.Count.ShouldBe(2);
        result.BatchFailedIds.ShouldBeEmpty();
        handler.Requests.Count.ShouldBe(2);
    }

    [Fact]
    public async Task FetchAsync_RetryAlsoFails_ReturnsBatchFailedIds()
    {
        using var handler = new RecordingHandler(
        [
            new FakeResponse(HttpStatusCode.InternalServerError, "boom"),
            new FakeResponse(HttpStatusCode.InternalServerError, "boom again"),
        ]);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsClient(httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        var result = await client.FetchAsync([10, 11], TestContext.Current.CancellationToken);

        result.Items.ShouldBeEmpty();
        result.BatchFailedIds.ShouldBe([10, 11]);
        handler.Requests.Count.ShouldBe(2);
    }

    [Fact]
    public async Task FetchAsync_TooManyRequestsWithRetryAfterZero_RetriesAndSucceeds()
    {
        var responseJson = BuildBatchResponse([10]);
        var rateLimited = new FakeResponse(HttpStatusCode.TooManyRequests, "slow down", RetryAfterSeconds: 0);

        using var handler = new RecordingHandler(
        [
            rateLimited,
            new FakeResponse(HttpStatusCode.OK, responseJson),
        ]);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsClient(httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        var result = await client.FetchAsync([10], TestContext.Current.CancellationToken);

        result.Items.Single().Id.ShouldBe(10);
        handler.Requests.Count.ShouldBe(2);
    }

    [Fact]
    public async Task FetchAsync_OneSubBatchFailsAnotherSucceeds_ReturnsPartialItemsAndBatchFailedIds()
    {
        var ids = Enumerable.Range(1, 250).ToArray();

        using var handler = new RecordingHandler(
        [
            new FakeResponse(HttpStatusCode.OK, BuildBatchResponse(Enumerable.Range(1, 200))),
            new FakeResponse(HttpStatusCode.InternalServerError, "boom"),
            new FakeResponse(HttpStatusCode.InternalServerError, "boom again"),
        ]);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsClient(httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        var result = await client.FetchAsync(ids, TestContext.Current.CancellationToken);

        result.Items.Count.ShouldBe(200);
        result.BatchFailedIds.Count.ShouldBe(50);
        result.BatchFailedIds.ShouldBe(Enumerable.Range(201, 50));
    }

    [Fact]
    public async Task FetchAsync_NonTransientStatus_Throws()
    {
        using var handler = new RecordingHandler([new FakeResponse(HttpStatusCode.BadRequest, "bad")]);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsClient(httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        await Should.ThrowAsync<HttpRequestException>(() => client.FetchAsync([10]));
        handler.Requests.Count.ShouldBe(1);
    }

    [Fact]
    public async Task FetchAsync_EmptyInput_ReturnsEmptyResultAndDoesNotCallServer()
    {
        using var handler = new RecordingHandler([]);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsClient(httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        var result = await client.FetchAsync([], TestContext.Current.CancellationToken);

        result.Items.ShouldBeEmpty();
        result.BatchFailedIds.ShouldBeEmpty();
        handler.Requests.ShouldBeEmpty();
    }

    private static string BuildBatchResponse(IEnumerable<int> ids)
    {
        var items = string.Join(",", ids.Select(id => $$"""
            { "id": {{id}}, "fields": { "System.WorkItemType": "Task", "System.Title": "item {{id}}", "System.State": "New" } }
            """));
        return $$"""
        {
          "count": {{ids.Count()}},
          "value": [{{items}}]
        }
        """;
    }
}

internal sealed record FakeResponse(HttpStatusCode Status, string Body, int? RetryAfterSeconds = null);

internal sealed record RecordedRequest(HttpMethod Method, string? Url, string? Body = null);

internal sealed class RecordingHandler : HttpMessageHandler
{
    private readonly Queue<FakeResponse> _responses;

    public RecordingHandler(IEnumerable<FakeResponse> responses)
    {
        _responses = new Queue<FakeResponse>(responses);
    }

    public List<RecordedRequest> Requests { get; } = new();

    public List<string> RequestBodies { get; } = new();

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(new RecordedRequest(request.Method, request.RequestUri?.ToString()));
        RequestBodies.Add(request.Content is not null
            ? await request.Content.ReadAsStringAsync(cancellationToken)
            : string.Empty);

        if (_responses.Count == 0)
        {
            throw new InvalidOperationException("RecordingHandler ran out of scripted responses.");
        }

        var next = _responses.Dequeue();
        var response = new HttpResponseMessage(next.Status)
        {
            Content = new StringContent(next.Body, System.Text.Encoding.UTF8, "application/json"),
        };
        if (next.RetryAfterSeconds is int secs)
        {
            response.Headers.Add("Retry-After", secs.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        return response;
    }
}
