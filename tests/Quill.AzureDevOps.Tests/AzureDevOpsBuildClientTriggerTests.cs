using System.Net;
using System.Text;
using System.Text.Json;
using Quill.Core;
using Quill.Core.Models;
using Quill.Tests.Shared;
using Shouldly;

namespace Quill.AzureDevOps.Tests;

public class AzureDevOpsBuildClientTriggerTests
{
    private const string QueuedAnswer = """{"id":4202,"state":"inProgress"}""";

    private const string RunAnswer = """
        {"id":4202,"buildNumber":"20261106.4","status":"notStarted","reason":"manual","sourceBranch":"refs/heads/main","sourceVersion":"3f2a9c1","queueTime":"2026-11-06T09:00:01.123Z","definition":{"id":42,"name":"Contoso PR Validation"},"requestedFor":{"displayName":"Jane Doe"},"_links":{"web":{"href":"https://devops.example.com/COL/PROJ/_build/results?buildId=4202"}}}
        """;

    private const string Base = $"{TestConstants.ServerUrl}/{TestConstants.Collection}/{TestConstants.Project}/_apis";

    [Fact]
    public async Task QueueRunAsync_PostsToPipelineRunsUrl()
    {
        // Arrange
        using var handler = SuccessHandler();
        using var httpClient = CreateHttpClient(handler);
        var client = CreateClient(httpClient);

        // Act
        await client.QueueRunAsync(new PipelineTriggerRequest { PipelineId = 42 }, TestContext.Current.CancellationToken);

        // Assert
        handler.Requests[0].Method.ShouldBe(HttpMethod.Post);
        handler.Requests[0].RequestUri!.ToString().ShouldBe($"{Base}/pipelines/42/runs?api-version=7.1-preview");
        handler.ContentTypes[0].ShouldBe("application/json");
    }

    [Fact]
    public async Task QueueRunAsync_ReadsTheNewRunFromTheBuildEndpoint()
    {
        // Arrange
        using var handler = SuccessHandler();
        using var httpClient = CreateHttpClient(handler);
        var client = CreateClient(httpClient);

        // Act
        var run = await client.QueueRunAsync(new PipelineTriggerRequest { PipelineId = 42 }, TestContext.Current.CancellationToken);

        // Assert
        handler.Requests.Count.ShouldBe(2);
        handler.Requests[1].Method.ShouldBe(HttpMethod.Get);
        handler.Requests[1].RequestUri!.ToString().ShouldBe($"{Base}/build/builds/4202?api-version=7.1-preview");
        run.Id.ShouldBe(4202);
        run.PipelineId.ShouldBe(42);
    }

    [Fact]
    public async Task QueueRunAsync_NoFlags_SendsEmptyObject()
    {
        // Arrange
        using var handler = SuccessHandler();
        using var httpClient = CreateHttpClient(handler);
        var client = CreateClient(httpClient);

        // Act
        await client.QueueRunAsync(new PipelineTriggerRequest { PipelineId = 42 }, TestContext.Current.CancellationToken);

        // Assert
        using var doc = JsonDocument.Parse(handler.Bodies[0]);
        doc.RootElement.ValueKind.ShouldBe(JsonValueKind.Object);
        doc.RootElement.EnumerateObject().ShouldBeEmpty();
    }

    [Theory]
    [InlineData("main", "refs/heads/main")]
    [InlineData("feature/login", "refs/heads/feature/login")]
    [InlineData("refs/heads/release/1.0", "refs/heads/release/1.0")]
    public async Task QueueRunAsync_Branch_SendsSelfRefName(string branch, string expected)
    {
        // Arrange
        using var handler = SuccessHandler();
        using var httpClient = CreateHttpClient(handler);
        var client = CreateClient(httpClient);

        // Act
        await client.QueueRunAsync(
            new PipelineTriggerRequest { PipelineId = 42, Branch = branch }, TestContext.Current.CancellationToken);

        // Assert
        using var doc = JsonDocument.Parse(handler.Bodies[0]);
        doc.RootElement.GetProperty("resources").GetProperty("repositories").GetProperty("self")
            .GetProperty("refName").GetString().ShouldBe(expected);
        doc.RootElement.TryGetProperty("templateParameters", out _).ShouldBeFalse();
        doc.RootElement.TryGetProperty("stagesToSkip", out _).ShouldBeFalse();
    }

    [Fact]
    public async Task QueueRunAsync_Parameters_SendsStringValues()
    {
        // Arrange
        using var handler = SuccessHandler();
        using var httpClient = CreateHttpClient(handler);
        var client = CreateClient(httpClient);
        var parameters = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["env"] = "dev",
            ["count"] = "3",
            ["empty"] = string.Empty,
            ["note"] = "Bär & <x> \"q\"",
        };

        // Act
        await client.QueueRunAsync(
            new PipelineTriggerRequest { PipelineId = 42, Parameters = parameters }, TestContext.Current.CancellationToken);

        // Assert
        using var doc = JsonDocument.Parse(handler.Bodies[0]);
        var sent = doc.RootElement.GetProperty("templateParameters");
        sent.EnumerateObject().Count().ShouldBe(4);
        sent.EnumerateObject().ShouldAllBe(property => property.Value.ValueKind == JsonValueKind.String);
        sent.GetProperty("count").GetString().ShouldBe("3");
        sent.GetProperty("empty").GetString().ShouldBe(string.Empty);
        sent.GetProperty("note").GetString().ShouldBe("Bär & <x> \"q\"");
        doc.RootElement.TryGetProperty("resources", out _).ShouldBeFalse();
    }

    [Fact]
    public async Task QueueRunAsync_StagesToSkip_SendsNamesInOrder()
    {
        // Arrange
        using var handler = SuccessHandler();
        using var httpClient = CreateHttpClient(handler);
        var client = CreateClient(httpClient);

        // Act
        await client.QueueRunAsync(
            new PipelineTriggerRequest { PipelineId = 42, StagesToSkip = ["Test", "Deploy"] },
            TestContext.Current.CancellationToken);

        // Assert
        using var doc = JsonDocument.Parse(handler.Bodies[0]);
        doc.RootElement.GetProperty("stagesToSkip").EnumerateArray().Select(item => item.GetString())
            .ShouldBe(["Test", "Deploy"]);
    }

    [Fact]
    public async Task QueueRunAsync_ReadFailsWith503_ThrowsUnavailableNamingTheQueuedRun()
    {
        // Arrange
        using var handler = new RecordingHandler(
        [
            new FakeResponse(HttpStatusCode.OK, """{"id":4202}"""),
            new FakeResponse(HttpStatusCode.ServiceUnavailable, string.Empty, RetryAfterSeconds: 7),
        ]);
        using var httpClient = TestHttp.Client(handler);
        var client = CreateClient(httpClient);

        // Act
        var exception = await Should.ThrowAsync<UnavailableException>(
            () => client.QueueRunAsync(new PipelineTriggerRequest { PipelineId = 42 }, TestContext.Current.CancellationToken));

        // Assert
        exception.Message.ShouldStartWith("Queued run 4202, but reading it failed: ");
        exception.RetryAfter.ShouldBe(TimeSpan.FromSeconds(7));
        exception.InnerException.ShouldBeOfType<UnavailableException>();
    }

    [Fact]
    public async Task QueueRunAsync_ReadFailsWith418_ThrowsDefectWithoutThePrefix()
    {
        // Arrange
        using var handler = new SequenceHttpHandler(
            (HttpStatusCode.OK, """{"id":4202}"""),
            ((HttpStatusCode)418, string.Empty));
        using var httpClient = CreateHttpClient(handler);
        var client = CreateClient(httpClient);

        // Act
        var exception = await Should.ThrowAsync<DefectException>(
            () => client.QueueRunAsync(new PipelineTriggerRequest { PipelineId = 42 }, TestContext.Current.CancellationToken));

        // Assert
        exception.Message.ShouldNotContain("Queued run");
    }

    [Fact]
    public async Task QueueRunAsync_InvalidStateChange500_ThrowsRejected()
    {
        // Arrange
        using var handler = new SequenceHttpHandler(
            (HttpStatusCode.InternalServerError, """{"message":"Pipeline 42 cannot be run because it is disabled.","typeKey":"BuildStatusInvalidChangeException"}"""));
        using var httpClient = CreateHttpClient(handler);
        var client = CreateClient(httpClient);

        // Act
        var exception = await Should.ThrowAsync<RejectedException>(
            () => client.QueueRunAsync(new PipelineTriggerRequest { PipelineId = 42 }, TestContext.Current.CancellationToken));

        // Assert
        exception.Message.ShouldBe("Pipeline 42 cannot be run because it is disabled. (500)");
        handler.Requests.Count.ShouldBe(1);
    }

    [Fact]
    public async Task QueueRunAsync_ReadFailsWith404_ThrowsRejectedNamingTheQueuedRun()
    {
        // Arrange
        using var handler = new SequenceHttpHandler(
            (HttpStatusCode.OK, """{"id":4202}"""),
            (HttpStatusCode.NotFound, string.Empty));
        using var httpClient = CreateHttpClient(handler);
        var client = CreateClient(httpClient);

        // Act
        var exception = await Should.ThrowAsync<RejectedException>(
            () => client.QueueRunAsync(new PipelineTriggerRequest { PipelineId = 42 }, TestContext.Current.CancellationToken));

        // Assert
        exception.Message.ShouldStartWith("Queued run 4202, but reading it failed: ");
        exception.InnerException.ShouldBeOfType<RejectedException>();
    }

    [Fact]
    public async Task QueueRunAsync_ReadFailsWith401_ThrowsDeniedNamingTheQueuedRun()
    {
        // Arrange
        using var handler = new SequenceHttpHandler(
            (HttpStatusCode.OK, """{"id":4202}"""),
            (HttpStatusCode.Unauthorized, string.Empty));
        using var httpClient = CreateHttpClient(handler);
        var client = CreateClient(httpClient);

        // Act
        var exception = await Should.ThrowAsync<DeniedException>(
            () => client.QueueRunAsync(new PipelineTriggerRequest { PipelineId = 42 }, TestContext.Current.CancellationToken));

        // Assert
        exception.Message.ShouldStartWith("Queued run 4202, but reading it failed: ");
        exception.InnerException.ShouldBeOfType<DeniedException>();
    }

    private static SequenceHttpHandler SuccessHandler() =>
        new((HttpStatusCode.OK, QueuedAnswer), (HttpStatusCode.OK, RunAnswer));

    private static HttpClient CreateHttpClient(SequenceHttpHandler handler) =>
        TestHttp.Client(handler);

    private static AzureDevOpsBuildClient CreateClient(HttpClient httpClient) =>
        new(httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);
}

internal sealed class SequenceHttpHandler : HttpMessageHandler
{
    private readonly Queue<(HttpStatusCode Status, string Body)> _answers;

    public SequenceHttpHandler(params (HttpStatusCode Status, string Body)[] answers)
    {
        _answers = new Queue<(HttpStatusCode, string)>(answers);
    }

    public List<HttpRequestMessage> Requests { get; } = [];

    public List<string> Bodies { get; } = [];

    public List<string?> ContentTypes { get; } = [];

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        Requests.Add(request);
        Bodies.Add(request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken));
        ContentTypes.Add(request.Content?.Headers.ContentType?.MediaType);

        var (status, body) = _answers.Dequeue();
        return new HttpResponseMessage(status)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };
    }
}
