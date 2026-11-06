using System.Net;
using Quill.Core.Models;
using Quill.Tests.Shared;
using Shouldly;

namespace Quill.AzureDevOps.Tests;

public class AzureDevOpsBuildClientTests
{
    private const string CompletedBuild = """
        {"id":121549,"buildNumber":"20261106.3","status":"completed","result":"failed","reason":"pullRequest","sourceBranch":"refs/pull/23085/merge","sourceVersion":"3f2a9c1","queueTime":"2026-11-06T07:58:01.123Z","startTime":"2026-11-06T07:58:20.5Z","finishTime":"2026-11-06T08:02:11.9Z","definition":{"id":42,"name":"G6 PR Validation Pipeline"},"requestedFor":{"displayName":"Raphael Fleischlin"},"_links":{"web":{"href":"https://devops.example.com/COL/PROJ/_build/results?buildId=121549"}}}
        """;

    private const string QueuedBuild = """
        {"id":121549,"buildNumber":"20261106.3","status":"notStarted","reason":"pullRequest","sourceBranch":"refs/pull/23085/merge","sourceVersion":"3f2a9c1","queueTime":"2026-11-06T07:58:01.123Z","definition":{"id":42,"name":"G6 PR Validation Pipeline"},"requestedFor":{"displayName":"Raphael Fleischlin"},"_links":{"web":{"href":"https://devops.example.com/COL/PROJ/_build/results?buildId=121549"}}}
        """;

    private const string PipelineList = """
        {"count":3,"value":[{"id":42,"name":"G6 PR Validation Pipeline","path":"\\"},{"id":43,"name":"G6 Nightly","path":"\\Team A"},{"id":44,"name":"G6 Release","path":"\\Team A\\CI"}]}
        """;

    private const string PipelineBase =
        $"{TestConstants.ServerUrl}/{TestConstants.Collection}/{TestConstants.Project}/_apis/build/definitions";

    [Fact]
    public async Task GetRunAsync_SendsBuildUrlWithApiVersion()
    {
        // Arrange
        using var handler = new FakeHttpHandler(CompletedBuild, HttpStatusCode.OK);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsBuildClient(
            httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        // Act
        await client.GetRunAsync(121549, TestContext.Current.CancellationToken);

        // Assert
        handler.LastRequest!.Method.ShouldBe(HttpMethod.Get);
        handler.LastRequest.RequestUri!.ToString().ShouldBe(
            $"{TestConstants.ServerUrl}/{TestConstants.Collection}/{TestConstants.Project}/_apis/build/builds/121549?api-version=7.1-preview");
    }

    [Fact]
    public async Task GetRunAsync_CompletedBuild_MapsEveryField()
    {
        // Arrange
        using var handler = new FakeHttpHandler(CompletedBuild, HttpStatusCode.OK);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsBuildClient(
            httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        // Act
        var run = await client.GetRunAsync(121549, TestContext.Current.CancellationToken);

        // Assert
        run.Id.ShouldBe(121549);
        run.PipelineId.ShouldBe(42);
        run.PipelineName.ShouldBe("G6 PR Validation Pipeline");
        run.BuildNumber.ShouldBe("20261106.3");
        run.Status.ShouldBe("completed");
        run.Result.ShouldBe("failed");
        run.Reason.ShouldBe("pullRequest");
        run.SourceBranch.ShouldBe("refs/pull/23085/merge");
        run.SourceVersion.ShouldBe("3f2a9c1");
        run.RequestedFor.ShouldBe("Raphael Fleischlin");
        run.QueueTime.ShouldBe(DateTimeOffset.Parse("2026-11-06T07:58:01.123Z", System.Globalization.CultureInfo.InvariantCulture));
        run.StartTime.ShouldBe(DateTimeOffset.Parse("2026-11-06T07:58:20.5Z", System.Globalization.CultureInfo.InvariantCulture));
        run.FinishTime.ShouldBe(DateTimeOffset.Parse("2026-11-06T08:02:11.9Z", System.Globalization.CultureInfo.InvariantCulture));
        run.WebUrl.ShouldBe("https://devops.example.com/COL/PROJ/_build/results?buildId=121549");
    }

    [Fact]
    public async Task GetRunAsync_QueuedBuild_LeavesResultStartTimeAndFinishTimeNull()
    {
        // Arrange
        using var handler = new FakeHttpHandler(QueuedBuild, HttpStatusCode.OK);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsBuildClient(
            httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        // Act
        var run = await client.GetRunAsync(121549, TestContext.Current.CancellationToken);

        // Assert
        run.Status.ShouldBe("notStarted");
        run.Result.ShouldBeNull();
        run.StartTime.ShouldBeNull();
        run.FinishTime.ShouldBeNull();
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.Unauthorized)]
    public async Task GetRunAsync_ErrorStatus_ThrowsHttpRequestExceptionWithStatus(HttpStatusCode status)
    {
        // Arrange
        using var handler = new FakeHttpHandler("{}", status);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsBuildClient(
            httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        // Act
        var act = () => client.GetRunAsync(121549, TestContext.Current.CancellationToken);

        // Assert
        var exception = await Should.ThrowAsync<HttpRequestException>(act);
        exception.StatusCode.ShouldBe(status);
    }

    [Fact]
    public async Task ListPipelinesAsync_NoFilter_SendsTopAndApiVersionOnly()
    {
        // Arrange
        using var handler = new FakeHttpHandler(PipelineList, HttpStatusCode.OK);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsBuildClient(
            httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        // Act
        await client.ListPipelinesAsync(new PipelineListQuery { Top = 50 }, TestContext.Current.CancellationToken);

        // Assert
        handler.LastRequest!.Method.ShouldBe(HttpMethod.Get);
        handler.LastRequest.RequestUri!.AbsoluteUri.ShouldBe($"{PipelineBase}?$top=50&api-version=7.1-preview");
    }

    [Fact]
    public async Task ListPipelinesAsync_NameAndFolder_SendsEscapedNameAndPath()
    {
        // Arrange
        using var handler = new FakeHttpHandler(PipelineList, HttpStatusCode.OK);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsBuildClient(
            httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        // Act
        await client.ListPipelinesAsync(
            new PipelineListQuery { Name = "G6*", Folder = "\\Team A\\CI", Top = 10 },
            TestContext.Current.CancellationToken);

        // Assert
        handler.LastRequest!.RequestUri!.AbsoluteUri.ShouldBe(
            $"{PipelineBase}?$top=10&api-version=7.1-preview&name=G6%2A&path=%5CTeam%20A%5CCI");
    }

    [Fact]
    public async Task ListPipelinesAsync_EmptyNameAndFolder_SendsNeither()
    {
        // Arrange
        using var handler = new FakeHttpHandler(PipelineList, HttpStatusCode.OK);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsBuildClient(
            httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        // Act
        await client.ListPipelinesAsync(
            new PipelineListQuery { Name = string.Empty, Folder = string.Empty, Top = 50 },
            TestContext.Current.CancellationToken);

        // Assert
        var url = handler.LastRequest!.RequestUri!.AbsoluteUri;
        url.ShouldNotContain("name=");
        url.ShouldNotContain("path=");
    }

    [Fact]
    public async Task ListPipelinesAsync_ThreePipelines_MapsEveryRowInServerOrder()
    {
        // Arrange
        using var handler = new FakeHttpHandler(PipelineList, HttpStatusCode.OK);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsBuildClient(
            httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        // Act
        var pipelines = await client.ListPipelinesAsync(
            new PipelineListQuery { Top = 50 }, TestContext.Current.CancellationToken);

        // Assert
        pipelines.Count.ShouldBe(3);
        pipelines[0].Id.ShouldBe(42);
        pipelines[0].Name.ShouldBe("G6 PR Validation Pipeline");
        pipelines[0].Path.ShouldBe("\\");
        pipelines[1].Id.ShouldBe(43);
        pipelines[1].Name.ShouldBe("G6 Nightly");
        pipelines[1].Path.ShouldBe("\\Team A");
        pipelines[2].Id.ShouldBe(44);
        pipelines[2].Name.ShouldBe("G6 Release");
        pipelines[2].Path.ShouldBe("\\Team A\\CI");
    }

    [Fact]
    public async Task ListPipelinesAsync_EmptyValue_ReturnsEmptyList()
    {
        // Arrange
        using var handler = new FakeHttpHandler("""{"count":0,"value":[]}""", HttpStatusCode.OK);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsBuildClient(
            httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        // Act
        var pipelines = await client.ListPipelinesAsync(
            new PipelineListQuery { Top = 50 }, TestContext.Current.CancellationToken);

        // Assert
        pipelines.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task ListPipelinesAsync_TopBelowOne_ThrowsArgumentOutOfRange(int top)
    {
        // Arrange
        using var handler = new FakeHttpHandler(PipelineList, HttpStatusCode.OK);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsBuildClient(
            httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        // Act
        var act = () => client.ListPipelinesAsync(
            new PipelineListQuery { Top = top }, TestContext.Current.CancellationToken);

        // Assert
        await Should.ThrowAsync<ArgumentOutOfRangeException>(act);
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.Unauthorized)]
    public async Task ListPipelinesAsync_ErrorStatus_ThrowsHttpRequestExceptionWithStatus(HttpStatusCode status)
    {
        // Arrange
        using var handler = new FakeHttpHandler("{}", status);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsBuildClient(
            httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        // Act
        var act = () => client.ListPipelinesAsync(
            new PipelineListQuery { Top = 50 }, TestContext.Current.CancellationToken);

        // Assert
        var exception = await Should.ThrowAsync<HttpRequestException>(act);
        exception.StatusCode.ShouldBe(status);
    }
}
