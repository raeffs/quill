using System.Net;
using System.Text;
using System.Text.Json;
using Quill.Core.Models;
using Quill.Tests.Shared;
using Shouldly;

namespace Quill.AzureDevOps.Tests;

public class AzureDevOpsPullRequestClientCreateTests
{
    private const string CreatedJson = """
    {
        "pullRequestId": 4711,
        "title": "Add the importer",
        "status": "active",
        "isDraft": true,
        "mergeStatus": "queued",
        "sourceRefName": "refs/heads/feature/importer",
        "targetRefName": "refs/heads/main",
        "creationDate": "2026-08-28T10:00:00Z",
        "createdBy": {"displayName": "Raphael Fleischlin"},
        "repository": {"name": "importer"},
        "reviewers": [],
        "labels": [],
        "description": "body"
    }
    """;

    [Fact]
    public async Task CreateAsync_PostsToRepositoryEndpointWithSupportsIterations()
    {
        // Arrange
        using var handler = new FakeHttpHandler(CreatedJson, HttpStatusCode.Created);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsPullRequestClient(
            httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        // Act
        await client.CreateAsync(
            new PullRequestCreateRequest
            {
                Repo = "importer",
                SourceBranch = "feature/importer",
                TargetBranch = "main",
                Title = "Add the importer",
            },
            TestContext.Current.CancellationToken);

        // Assert
        handler.LastRequest!.Method.ShouldBe(HttpMethod.Post);
        var url = handler.LastRequest.RequestUri!.ToString();
        url.ShouldStartWith($"{TestConstants.ServerUrl}/{TestConstants.Collection}/{TestConstants.Project}/_apis/git/repositories/importer/pullrequests?");
        url.ShouldContain("supportsIterations=true");
    }

    [Fact]
    public async Task CreateAsync_SendsDraftWithFullRefNames()
    {
        // Arrange
        using var handler = new FakeHttpHandler(CreatedJson, HttpStatusCode.Created);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsPullRequestClient(
            httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        // Act
        await client.CreateAsync(
            new PullRequestCreateRequest
            {
                Repo = "importer",
                SourceBranch = "feature/importer",
                TargetBranch = "refs/heads/main",
                Title = "Add the importer",
            },
            TestContext.Current.CancellationToken);

        // Assert
        using var body = JsonDocument.Parse(handler.LastRequestBody!);
        var root = body.RootElement;
        root.GetProperty("sourceRefName").GetString().ShouldBe("refs/heads/feature/importer");
        root.GetProperty("targetRefName").GetString().ShouldBe("refs/heads/main");
        root.GetProperty("title").GetString().ShouldBe("Add the importer");
        root.GetProperty("isDraft").GetBoolean().ShouldBeTrue();
    }

    [Fact]
    public async Task CreateAsync_NoDescription_OmitsTheKey()
    {
        // Arrange
        using var handler = new FakeHttpHandler(CreatedJson, HttpStatusCode.Created);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsPullRequestClient(
            httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        // Act
        await client.CreateAsync(
            new PullRequestCreateRequest
            {
                Repo = "importer",
                SourceBranch = "feature/importer",
                TargetBranch = "main",
                Title = "Add the importer",
            },
            TestContext.Current.CancellationToken);

        // Assert
        using var body = JsonDocument.Parse(handler.LastRequestBody!);
        body.RootElement.TryGetProperty("description", out _).ShouldBeFalse();
        body.RootElement.TryGetProperty("workItemRefs", out _).ShouldBeFalse();
    }

    [Fact]
    public async Task CreateAsync_SendsDescriptionAndWorkItemRefs()
    {
        // Arrange
        using var handler = new FakeHttpHandler(CreatedJson, HttpStatusCode.Created);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsPullRequestClient(
            httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        // Act
        await client.CreateAsync(
            new PullRequestCreateRequest
            {
                Repo = "importer",
                SourceBranch = "feature/importer",
                TargetBranch = "main",
                Title = "Add the importer",
                Description = "## Why\n\nBecause.",
                WorkItemIds = [63480, 63481],
            },
            TestContext.Current.CancellationToken);

        // Assert
        using var body = JsonDocument.Parse(handler.LastRequestBody!);
        var root = body.RootElement;
        root.GetProperty("description").GetString().ShouldBe("## Why\n\nBecause.");
        root.GetProperty("workItemRefs").EnumerateArray()
            .Select(e => e.GetProperty("id").GetString())
            .ShouldBe(["63480", "63481"]);
    }

    [Fact]
    public async Task CreateAsync_MapsTheCreatedPullRequest()
    {
        // Arrange
        using var handler = new FakeHttpHandler(CreatedJson, HttpStatusCode.Created);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsPullRequestClient(
            httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        // Act
        var pullRequest = await client.CreateAsync(
            new PullRequestCreateRequest
            {
                Repo = "importer",
                SourceBranch = "feature/importer",
                TargetBranch = "main",
                Title = "Add the importer",
            },
            TestContext.Current.CancellationToken);

        // Assert
        pullRequest.Id.ShouldBe(4711);
        pullRequest.IsDraft.ShouldBeTrue();
        pullRequest.SourceBranch.ShouldBe("feature/importer");
        pullRequest.TargetBranch.ShouldBe("main");
        pullRequest.MergeStatus.ShouldBe("queued");
        pullRequest.WebUrl.ShouldBe(
            $"{TestConstants.ServerUrl}/{TestConstants.Collection}/{TestConstants.Project}/_git/importer/pullrequest/4711");
    }

    [Fact]
    public async Task CreateAsync_NoTargetBranch_ReadsTheRepositoryDefaultBranch()
    {
        // Arrange
        using var handler = new ScriptedCreateHandler(["""{"defaultBranch":"refs/heads/develop"}""", CreatedJson]);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsPullRequestClient(
            httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        // Act
        await client.CreateAsync(
            new PullRequestCreateRequest
            {
                Repo = "importer",
                SourceBranch = "feature/importer",
                Title = "Add the importer",
            },
            TestContext.Current.CancellationToken);

        // Assert
        handler.Requests.Count.ShouldBe(2);
        handler.Requests[0].Method.ShouldBe(HttpMethod.Get);
        handler.Requests[0].Url!.ShouldStartWith(
            $"{TestConstants.ServerUrl}/{TestConstants.Collection}/{TestConstants.Project}/_apis/git/repositories/importer?");

        using var body = JsonDocument.Parse(handler.Requests[1].Body!);
        body.RootElement.GetProperty("targetRefName").GetString().ShouldBe("refs/heads/develop");
    }

    [Fact]
    public async Task CreateAsync_TargetBranchGiven_SkipsTheRepositoryRead()
    {
        // Arrange
        using var handler = new ScriptedCreateHandler([CreatedJson]);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsPullRequestClient(
            httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        // Act
        await client.CreateAsync(
            new PullRequestCreateRequest
            {
                Repo = "importer",
                SourceBranch = "feature/importer",
                TargetBranch = "main",
                Title = "Add the importer",
            },
            TestContext.Current.CancellationToken);

        // Assert
        handler.Requests.Count.ShouldBe(1);
        handler.Requests[0].Method.ShouldBe(HttpMethod.Post);
    }

    [Fact]
    public async Task CreateAsync_RepositoryHasNoDefaultBranch_ThrowsInvalidOperationException()
    {
        // Arrange
        using var handler = new ScriptedCreateHandler(["""{"name":"importer"}""", CreatedJson]);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsPullRequestClient(
            httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        // Act
        Func<Task> act = () => client.CreateAsync(
            new PullRequestCreateRequest
            {
                Repo = "importer",
                SourceBranch = "feature/importer",
                Title = "Add the importer",
            },
            TestContext.Current.CancellationToken);

        // Assert
        await Should.ThrowAsync<InvalidOperationException>(act);
    }

    [Fact]
    public async Task CreateAsync_ServerRejects_SurfacesTheServerMessage()
    {
        // Arrange
        var errorJson = """
        {
            "message": "Invalid argument value.\r\nParameter name: A description for a pull request must not be longer than 4000 characters.",
            "typeKey": "InvalidArgumentValueException"
        }
        """;
        using var handler = new FakeHttpHandler(errorJson, HttpStatusCode.BadRequest);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsPullRequestClient(
            httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        // Act
        Func<Task> act = () => client.CreateAsync(
            new PullRequestCreateRequest
            {
                Repo = "importer",
                SourceBranch = "feature/importer",
                TargetBranch = "main",
                Title = "Add the importer",
            },
            TestContext.Current.CancellationToken);

        // Assert
        var ex = await Should.ThrowAsync<HttpRequestException>(act);
        ex.Message.ShouldContain("must not be longer than 4000 characters");
    }

    [Fact]
    public async Task CreateAsync_ServerRejectsWithNoBody_ReportsTheStatusCode()
    {
        // Arrange
        using var handler = new FakeHttpHandler(string.Empty, HttpStatusCode.Unauthorized);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsPullRequestClient(
            httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        // Act
        Func<Task> act = () => client.CreateAsync(
            new PullRequestCreateRequest
            {
                Repo = "importer",
                SourceBranch = "feature/importer",
                TargetBranch = "main",
                Title = "Add the importer",
            },
            TestContext.Current.CancellationToken);

        // Assert
        var ex = await Should.ThrowAsync<HttpRequestException>(act);
        ex.Message.ShouldContain("401");
    }
}

internal sealed class ScriptedCreateHandler : HttpMessageHandler
{
    private readonly Queue<string> _responses;

    public ScriptedCreateHandler(IEnumerable<string> responses)
    {
        _responses = new Queue<string>(responses);
    }

    public List<RecordedRequest> Requests { get; } = new();

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(new RecordedRequest(
            request.Method,
            request.RequestUri?.ToString(),
            request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken)));

        var body = _responses.Count > 0 ? _responses.Dequeue() : "{}";
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };
    }
}
