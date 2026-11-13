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

    private const string CancelInvalidStateChangeBody = """
        {"$id":"1","innerException":null,"message":"Run 4202 cannot be cancelled because it has completed.","typeName":"Microsoft.TeamFoundation.Build.WebApi.BuildStatusInvalidChangeException, Microsoft.TeamFoundation.Build2.WebApi","typeKey":"BuildStatusInvalidChangeException","errorCode":0,"eventId":3000}
        """;

    private const string RetryInvalidStateChangeBody = """
        {"$id":"1","innerException":null,"message":"Unable to retry pipeline 20261101.7 because it is not currently in the completed state.","typeName":"Microsoft.TeamFoundation.Build.WebApi.BuildStatusInvalidChangeException, Microsoft.TeamFoundation.Build2.WebApi","typeKey":"BuildStatusInvalidChangeException","errorCode":0,"eventId":3000}
        """;

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

    [Fact]
    public async Task CancelRunAsync_InvalidStateChange500_ThrowsRejected()
    {
        // Arrange
        using var handler = new SequenceHttpHandler((HttpStatusCode.InternalServerError, CancelInvalidStateChangeBody));
        using var httpClient = CreateHttpClient(handler);
        var client = CreateClient(httpClient);

        // Act
        var exception = await Should.ThrowAsync<RejectedException>(
            () => client.CancelRunAsync(4202, TestContext.Current.CancellationToken));

        // Assert
        exception.Message.ShouldBe("Run 4202 cannot be cancelled because it has completed. (500)");
    }

    [Fact]
    public async Task RetryRunAsync_InvalidStateChange500_ThrowsRejected()
    {
        // Arrange
        using var handler = new SequenceHttpHandler((HttpStatusCode.InternalServerError, RetryInvalidStateChangeBody));
        using var httpClient = CreateHttpClient(handler);
        var client = CreateClient(httpClient);

        // Act
        var exception = await Should.ThrowAsync<RejectedException>(
            () => client.RetryRunAsync(4202, TestContext.Current.CancellationToken));

        // Assert
        exception.Message.ShouldBe("Unable to retry pipeline 20261101.7 because it is not currently in the completed state. (500)");
    }

    [Fact]
    public async Task CancelRunAsync_InvalidStateChange503_ThrowsUnavailable()
    {
        // Arrange
        using var handler = new SequenceHttpHandler((HttpStatusCode.ServiceUnavailable, CancelInvalidStateChangeBody));
        using var httpClient = CreateHttpClient(handler);
        var client = CreateClient(httpClient);

        // Act
        var exception = await Should.ThrowAsync<UnavailableException>(
            () => client.CancelRunAsync(4202, TestContext.Current.CancellationToken));

        // Assert
        exception.Message.ShouldBe("Run 4202 cannot be cancelled because it has completed. (503)");
    }

    [Fact]
    public async Task CancelRunAsync_Other500_ThrowsUnavailable()
    {
        // Arrange
        using var handler = new SequenceHttpHandler((HttpStatusCode.InternalServerError, """{"message":"Something broke.","typeKey":"SomeOtherException"}"""));
        using var httpClient = CreateHttpClient(handler);
        var client = CreateClient(httpClient);

        // Act
        var exception = await Should.ThrowAsync<UnavailableException>(
            () => client.CancelRunAsync(4202, TestContext.Current.CancellationToken));

        // Assert
        exception.Message.ShouldBe("Something broke. (500)");
    }

    [Fact]
    public async Task CancelRunAsync_NotFound_ThrowsRejected()
    {
        // Arrange
        using var handler = new SequenceHttpHandler((HttpStatusCode.NotFound, string.Empty));
        using var httpClient = CreateHttpClient(handler);
        var client = CreateClient(httpClient);

        // Act
        var exception = await Should.ThrowAsync<RejectedException>(
            () => client.CancelRunAsync(4202, TestContext.Current.CancellationToken));

        // Assert
        exception.Message.ShouldBe("The server answered 404 (Not Found).");
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
        TestHttp.Client(handler);

    private static AzureDevOpsBuildClient CreateClient(HttpClient httpClient) =>
        new(httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);
}
