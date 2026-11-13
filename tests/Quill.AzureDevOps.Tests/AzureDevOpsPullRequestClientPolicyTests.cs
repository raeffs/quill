using System.Net;
using System.Text.Json;
using Quill.Core;
using Quill.Tests.Shared;
using Shouldly;

namespace Quill.AzureDevOps.Tests;

public class AzureDevOpsPullRequestClientPolicyTests
{
    private const string ProjectId = "11111111-2222-3333-4444-555555555555";

    private const string EvaluationList = """
    {
        "count": 4,
        "value": [
            {
                "status": "rejected",
                "startedDate": "2026-05-12T08:00:00Z",
                "completedDate": "2026-05-12T08:10:00Z",
                "configuration": {
                    "isBlocking": true,
                    "type": { "displayName": "Build" }
                },
                "context": { "buildId": 4201, "buildOutputPreview": "error CS0103: The name does not exist" }
            },
            {
                "status": "approved",
                "configuration": {
                    "isBlocking": false,
                    "type": { "displayName": "Minimum number of reviewers" }
                },
                "context": {}
            },
            {
                "status": "queued",
                "startedDate": "2026-05-12T09:00:00Z",
                "configuration": {
                    "isBlocking": true,
                    "type": { "displayName": "Build" }
                },
                "context": null
            },
            {
                "status": "approved",
                "configuration": {
                    "isBlocking": true,
                    "type": { "displayName": "Comment requirements" }
                }
            }
        ]
    }
    """;

    private const string EmptyList = """{"count":0,"value":[]}""";

    [Fact]
    public async Task GetPolicyEvaluationsAsync_SendsCodeReviewArtifactId()
    {
        using var handler = new FakeHttpHandler(EmptyList, HttpStatusCode.OK);
        using var httpClient = CreateHttpClient(handler);
        var client = CreateClient(httpClient);

        await client.GetPolicyEvaluationsAsync(ProjectId, 9001, TestContext.Current.CancellationToken);

        handler.LastRequest!.Method.ShouldBe(HttpMethod.Get);
        var url = handler.LastRequest.RequestUri!.ToString();
        url.ShouldBe($"{TestConstants.ServerUrl}/{TestConstants.Collection}/{TestConstants.Project}/_apis/policy/evaluations?artifactId=vstfs:///CodeReview/CodeReviewId/{ProjectId}/9001&api-version=7.1-preview");
        url.ShouldNotContain("includeNotApplicable");
    }

    [Fact]
    public async Task GetPolicyEvaluationsAsync_MapsEveryField()
    {
        using var handler = new FakeHttpHandler(EvaluationList, HttpStatusCode.OK);
        using var httpClient = CreateHttpClient(handler);
        var client = CreateClient(httpClient);

        var result = await client.GetPolicyEvaluationsAsync(ProjectId, 9001, TestContext.Current.CancellationToken);

        var build = result[0];
        build.Name.ShouldBe("Build");
        build.IsBlocking.ShouldBeTrue();
        build.Status.ShouldBe("rejected");
        build.StartedDate.ShouldBe(DateTimeOffset.Parse("2026-05-12T08:00:00Z", System.Globalization.CultureInfo.InvariantCulture));
        build.CompletedDate.ShouldBe(DateTimeOffset.Parse("2026-05-12T08:10:00Z", System.Globalization.CultureInfo.InvariantCulture));
        build.Context!.Value.GetProperty("buildId").GetInt32().ShouldBe(4201);
        build.Context.Value.GetProperty("buildOutputPreview").GetString().ShouldNotBeNull();
    }

    [Fact]
    public async Task GetPolicyEvaluationsAsync_ContextForms_PassThrough()
    {
        using var handler = new FakeHttpHandler(EvaluationList, HttpStatusCode.OK);
        using var httpClient = CreateHttpClient(handler);
        var client = CreateClient(httpClient);

        var result = await client.GetPolicyEvaluationsAsync(ProjectId, 9001, TestContext.Current.CancellationToken);

        result[1].Context!.Value.GetRawText().ShouldBe("{}");
        result[2].Context.ShouldNotBeNull();
        result[2].Context!.Value.ValueKind.ShouldBe(JsonValueKind.Null);
        result[2].CompletedDate.ShouldBeNull();
        result[3].Context.ShouldBeNull();
    }

    [Fact]
    public async Task GetPolicyEvaluationsAsync_KeepsTheServerOrder()
    {
        using var handler = new FakeHttpHandler(EvaluationList, HttpStatusCode.OK);
        using var httpClient = CreateHttpClient(handler);
        var client = CreateClient(httpClient);

        var result = await client.GetPolicyEvaluationsAsync(ProjectId, 9001, TestContext.Current.CancellationToken);

        result.Select(e => e.Name).ShouldBe(["Build", "Minimum number of reviewers", "Build", "Comment requirements"]);
        result.Select(e => e.Status).ShouldBe(["rejected", "approved", "queued", "approved"]);
    }

    [Fact]
    public async Task GetPolicyEvaluationsAsync_EmptyValue_ReturnsEmptyList()
    {
        using var handler = new FakeHttpHandler(EmptyList, HttpStatusCode.OK);
        using var httpClient = CreateHttpClient(handler);
        var client = CreateClient(httpClient);

        var result = await client.GetPolicyEvaluationsAsync(ProjectId, 9001, TestContext.Current.CancellationToken);

        result.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, typeof(DeniedException))]
    [InlineData(HttpStatusCode.NotFound, typeof(RejectedException))]
    [InlineData(HttpStatusCode.InternalServerError, typeof(UnavailableException))]
    public async Task GetPolicyEvaluationsAsync_ErrorStatus_ThrowsTheModeOfTheStatus(HttpStatusCode status, Type mode)
    {
        using var handler = new FakeHttpHandler("{}", status);
        using var httpClient = CreateHttpClient(handler);
        var client = CreateClient(httpClient);

        var ex = await Should.ThrowAsync<QuillException>(
            () => client.GetPolicyEvaluationsAsync(ProjectId, 9001, TestContext.Current.CancellationToken));

        ex.ShouldBeOfType(mode);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task GetPolicyEvaluationsAsync_BlankProjectId_Throws(string projectId)
    {
        using var handler = new FakeHttpHandler(EmptyList, HttpStatusCode.OK);
        using var httpClient = CreateHttpClient(handler);
        var client = CreateClient(httpClient);

        await Should.ThrowAsync<ArgumentException>(
            () => client.GetPolicyEvaluationsAsync(projectId, 9001, TestContext.Current.CancellationToken));

        handler.RequestCount.ShouldBe(0);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetPolicyEvaluationsAsync_CodeReviewIdBelowOne_Throws(int codeReviewId)
    {
        using var handler = new FakeHttpHandler(EmptyList, HttpStatusCode.OK);
        using var httpClient = CreateHttpClient(handler);
        var client = CreateClient(httpClient);

        await Should.ThrowAsync<ArgumentOutOfRangeException>(
            () => client.GetPolicyEvaluationsAsync(ProjectId, codeReviewId, TestContext.Current.CancellationToken));

        handler.RequestCount.ShouldBe(0);
    }

    private static HttpClient CreateHttpClient(FakeHttpHandler handler) =>
        TestHttp.Client(handler);

    private static AzureDevOpsPullRequestClient CreateClient(HttpClient httpClient) =>
        new(httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);
}
