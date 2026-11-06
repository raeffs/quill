using System.Net;
using System.Text.Json;
using Quill.Core;
using Quill.Tests.Shared;
using Shouldly;

namespace Quill.AzureDevOps.Tests;

public class AzureDevOpsBuildClientUpdateTests
{
    private const string BuildAnswer = """
        {"id":4202,"buildNumber":"20261106.4","status":"cancelling","reason":"manual","sourceBranch":"refs/heads/main","sourceVersion":"3f2a9c1","queueTime":"2026-11-06T09:00:01.123Z","definition":{"id":42,"name":"Contoso PR Validation"},"requestedFor":{"displayName":"Jane Doe"},"_links":{"web":{"href":"https://devops.example.com/COL/PROJ/_build/results?buildId=4202"}}}
        """;

    private const string Base = $"{TestConstants.ServerUrl}/{TestConstants.Collection}/{TestConstants.Project}/_apis";

    [Fact]
    public async Task CancelRunAsync_PatchesTheBuild()
    {
        // Arrange
        using var handler = new SequenceHttpHandler((HttpStatusCode.OK, BuildAnswer));
        using var httpClient = CreateHttpClient(handler);
        var client = CreateClient(httpClient);

        // Act
        await client.CancelRunAsync(4202, TestContext.Current.CancellationToken);

        // Assert
        handler.Requests.Count.ShouldBe(1);
        handler.Requests[0].Method.ShouldBe(HttpMethod.Patch);
        handler.Requests[0].RequestUri!.ToString().ShouldBe($"{Base}/build/builds/4202?api-version=7.1-preview");
        handler.ContentTypes[0].ShouldBe("application/json");
    }

    [Fact]
    public async Task CancelRunAsync_SendsStatusCancelling()
    {
        // Arrange
        using var handler = new SequenceHttpHandler((HttpStatusCode.OK, BuildAnswer));
        using var httpClient = CreateHttpClient(handler);
        var client = CreateClient(httpClient);

        // Act
        await client.CancelRunAsync(4202, TestContext.Current.CancellationToken);

        // Assert
        using var doc = JsonDocument.Parse(handler.Bodies[0]);
        doc.RootElement.EnumerateObject().Count().ShouldBe(1);
        doc.RootElement.GetProperty("status").GetString().ShouldBe("cancelling");
    }

    [Fact]
    public async Task CancelRunAsync_ReturnsTheRunFromTheResponse()
    {
        // Arrange
        using var handler = new SequenceHttpHandler((HttpStatusCode.OK, BuildAnswer));
        using var httpClient = CreateHttpClient(handler);
        var client = CreateClient(httpClient);

        // Act
        var run = await client.CancelRunAsync(4202, TestContext.Current.CancellationToken);

        // Assert
        handler.Requests.Count.ShouldBe(1);
        run.Id.ShouldBe(4202);
        run.Status.ShouldBe("cancelling");
        run.PipelineId.ShouldBe(42);
        run.RequestedFor.ShouldBe("Jane Doe");
    }

    [Fact]
    public async Task RetryRunAsync_PatchesTheBuildWithRetryFlag()
    {
        // Arrange
        using var handler = new SequenceHttpHandler((HttpStatusCode.OK, BuildAnswer));
        using var httpClient = CreateHttpClient(handler);
        var client = CreateClient(httpClient);

        // Act
        await client.RetryRunAsync(4202, TestContext.Current.CancellationToken);

        // Assert
        handler.Requests.Count.ShouldBe(1);
        handler.Requests[0].Method.ShouldBe(HttpMethod.Patch);
        handler.Requests[0].RequestUri!.ToString().ShouldBe($"{Base}/build/builds/4202?api-version=7.1-preview&retry=true");
        handler.ContentTypes[0].ShouldBe("application/json");
    }

    [Fact]
    public async Task RetryRunAsync_SendsEmptyObject()
    {
        // Arrange
        using var handler = new SequenceHttpHandler((HttpStatusCode.OK, BuildAnswer));
        using var httpClient = CreateHttpClient(handler);
        var client = CreateClient(httpClient);

        // Act
        await client.RetryRunAsync(4202, TestContext.Current.CancellationToken);

        // Assert
        using var doc = JsonDocument.Parse(handler.Bodies[0]);
        doc.RootElement.ValueKind.ShouldBe(JsonValueKind.Object);
        doc.RootElement.EnumerateObject().ShouldBeEmpty();
    }

    [Fact]
    public async Task RetryRunAsync_ReturnsTheRunFromTheResponse()
    {
        // Arrange
        using var handler = new SequenceHttpHandler(
            (HttpStatusCode.OK, BuildAnswer.Replace("cancelling", "inProgress", StringComparison.Ordinal)));
        using var httpClient = CreateHttpClient(handler);
        var client = CreateClient(httpClient);

        // Act
        var run = await client.RetryRunAsync(4202, TestContext.Current.CancellationToken);

        // Assert
        handler.Requests.Count.ShouldBe(1);
        run.Status.ShouldBe("inProgress");
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Conflict)]
    public async Task RetryRunAsync_Rejected_ThrowsPipelineRejectedExceptionWithServerMessage(HttpStatusCode status)
    {
        // Arrange
        using var handler = new SequenceHttpHandler((status, """{"message":"The build is in progress and cannot be retried."}"""));
        using var httpClient = CreateHttpClient(handler);
        var client = CreateClient(httpClient);

        // Act
        var exception = await Should.ThrowAsync<PipelineRejectedException>(
            () => client.RetryRunAsync(4202, TestContext.Current.CancellationToken));

        // Assert
        exception.Message.ShouldBe($"The build is in progress and cannot be retried. ({(int)status})");
    }

    [Fact]
    public async Task CancelRunAsync_RejectedWithoutMessage_ThrowsRejectionNamingTheRun()
    {
        // Arrange
        using var handler = new SequenceHttpHandler((HttpStatusCode.BadRequest, string.Empty));
        using var httpClient = CreateHttpClient(handler);
        var client = CreateClient(httpClient);

        // Act
        var exception = await Should.ThrowAsync<PipelineRejectedException>(
            () => client.CancelRunAsync(4202, TestContext.Current.CancellationToken));

        // Assert
        exception.Message.ShouldBe("The server rejected the cancel of run 4202 (400)");
    }

    [Fact]
    public async Task RetryRunAsync_RejectedWithoutMessage_ThrowsRejectionNamingTheRun()
    {
        // Arrange
        using var handler = new SequenceHttpHandler((HttpStatusCode.Conflict, "not json"));
        using var httpClient = CreateHttpClient(handler);
        var client = CreateClient(httpClient);

        // Act
        var exception = await Should.ThrowAsync<PipelineRejectedException>(
            () => client.RetryRunAsync(4202, TestContext.Current.CancellationToken));

        // Assert
        exception.Message.ShouldBe("The server rejected the retry of run 4202 (409)");
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task CancelRunAsync_ApiError_ThrowsPlainHttpRequestException(HttpStatusCode status)
    {
        // Arrange
        using var handler = new SequenceHttpHandler((status, """{"message":"Some server message."}"""));
        using var httpClient = CreateHttpClient(handler);
        var client = CreateClient(httpClient);

        // Act
        var exception = await Should.ThrowAsync<HttpRequestException>(
            () => client.CancelRunAsync(4202, TestContext.Current.CancellationToken));

        // Assert
        exception.GetType().ShouldBe(typeof(HttpRequestException));
        exception.StatusCode.ShouldBe(status);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task RetryRunAsync_RunIdZeroOrNegative_Throws(int runId)
    {
        // Arrange
        using var handler = new SequenceHttpHandler();
        using var httpClient = CreateHttpClient(handler);
        var client = CreateClient(httpClient);

        // Act
        await Should.ThrowAsync<ArgumentOutOfRangeException>(
            () => client.RetryRunAsync(runId, TestContext.Current.CancellationToken));

        // Assert
        handler.Requests.ShouldBeEmpty();
    }

    private static HttpClient CreateHttpClient(SequenceHttpHandler handler) =>
        new(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };

    private static AzureDevOpsBuildClient CreateClient(HttpClient httpClient) =>
        new(httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);
}
