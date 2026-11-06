using System.Net;
using Quill.Core.Models;
using Quill.Tests.Shared;
using Shouldly;

namespace Quill.AzureDevOps.Tests;

public class AzureDevOpsBuildClientTests
{
    private const string CompletedBuild = """
        {"id":4201,"buildNumber":"20261106.3","status":"completed","result":"failed","reason":"pullRequest","sourceBranch":"refs/pull/815/merge","sourceVersion":"3f2a9c1","queueTime":"2026-11-06T07:58:01.123Z","startTime":"2026-11-06T07:58:20.5Z","finishTime":"2026-11-06T08:02:11.9Z","definition":{"id":42,"name":"Contoso PR Validation"},"requestedFor":{"displayName":"Jane Doe"},"_links":{"web":{"href":"https://devops.example.com/COL/PROJ/_build/results?buildId=4201"}}}
        """;

    private const string QueuedBuild = """
        {"id":4201,"buildNumber":"20261106.3","status":"notStarted","reason":"pullRequest","sourceBranch":"refs/pull/815/merge","sourceVersion":"3f2a9c1","queueTime":"2026-11-06T07:58:01.123Z","definition":{"id":42,"name":"Contoso PR Validation"},"requestedFor":{"displayName":"Jane Doe"},"_links":{"web":{"href":"https://devops.example.com/COL/PROJ/_build/results?buildId=4201"}}}
        """;

    private const string PipelineList = """
        {"count":3,"value":[{"id":42,"name":"Contoso PR Validation","path":"\\"},{"id":43,"name":"Contoso Nightly","path":"\\Team A"},{"id":44,"name":"Contoso Release","path":"\\Team A\\CI"}]}
        """;

    private const string PipelineBase =
        $"{TestConstants.ServerUrl}/{TestConstants.Collection}/{TestConstants.Project}/_apis/build/definitions";

    private const string RunList = """
        {"count":2,"value":[{"id":4201,"buildNumber":"20261106.3","status":"completed","result":"failed","reason":"pullRequest","sourceBranch":"refs/pull/815/merge","sourceVersion":"3f2a9c1","queueTime":"2026-11-06T07:58:01.123Z","startTime":"2026-11-06T07:58:20.5Z","finishTime":"2026-11-06T08:02:11.9Z","definition":{"id":42,"name":"Contoso PR Validation"},"requestedFor":{"displayName":"Jane Doe"},"_links":{"web":{"href":"https://devops.example.com/COL/PROJ/_build/results?buildId=4201"}}},{"id":4200,"buildNumber":"20261106.2","status":"inProgress","reason":"manual","sourceBranch":"refs/heads/main","sourceVersion":"9b8c7d6","queueTime":"2026-11-06T07:30:00Z","startTime":"2026-11-06T07:30:10Z","definition":{"id":42,"name":"Contoso PR Validation"},"requestedFor":{"displayName":"Jane Doe"},"_links":{"web":{"href":"https://devops.example.com/COL/PROJ/_build/results?buildId=4200"}}}]}
        """;

    private const string LogList = """
        {"count":3,"value":[{"id":1,"lineCount":40},{"id":12,"lineCount":1001},{"id":13,"lineCount":7}]}
        """;

    private const string RunBase =
        $"{TestConstants.ServerUrl}/{TestConstants.Collection}/{TestConstants.Project}/_apis/build/builds";

    [Fact]
    public async Task GetRunAsync_SendsBuildUrlWithApiVersion()
    {
        // Arrange
        using var handler = new FakeHttpHandler(CompletedBuild, HttpStatusCode.OK);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsBuildClient(
            httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        // Act
        await client.GetRunAsync(4201, TestContext.Current.CancellationToken);

        // Assert
        handler.LastRequest!.Method.ShouldBe(HttpMethod.Get);
        handler.LastRequest.RequestUri!.ToString().ShouldBe(
            $"{TestConstants.ServerUrl}/{TestConstants.Collection}/{TestConstants.Project}/_apis/build/builds/4201?api-version=7.1-preview");
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
        var run = await client.GetRunAsync(4201, TestContext.Current.CancellationToken);

        // Assert
        run.Id.ShouldBe(4201);
        run.PipelineId.ShouldBe(42);
        run.PipelineName.ShouldBe("Contoso PR Validation");
        run.BuildNumber.ShouldBe("20261106.3");
        run.Status.ShouldBe("completed");
        run.Result.ShouldBe("failed");
        run.Reason.ShouldBe("pullRequest");
        run.SourceBranch.ShouldBe("refs/pull/815/merge");
        run.SourceVersion.ShouldBe("3f2a9c1");
        run.RequestedFor.ShouldBe("Jane Doe");
        run.QueueTime.ShouldBe(DateTimeOffset.Parse("2026-11-06T07:58:01.123Z", System.Globalization.CultureInfo.InvariantCulture));
        run.StartTime.ShouldBe(DateTimeOffset.Parse("2026-11-06T07:58:20.5Z", System.Globalization.CultureInfo.InvariantCulture));
        run.FinishTime.ShouldBe(DateTimeOffset.Parse("2026-11-06T08:02:11.9Z", System.Globalization.CultureInfo.InvariantCulture));
        run.WebUrl.ShouldBe("https://devops.example.com/COL/PROJ/_build/results?buildId=4201");
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
        var run = await client.GetRunAsync(4201, TestContext.Current.CancellationToken);

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
        var act = () => client.GetRunAsync(4201, TestContext.Current.CancellationToken);

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
            new PipelineListQuery { Name = "Contoso*", Folder = "\\Team A\\CI", Top = 10 },
            TestContext.Current.CancellationToken);

        // Assert
        handler.LastRequest!.RequestUri!.AbsoluteUri.ShouldBe(
            $"{PipelineBase}?$top=10&api-version=7.1-preview&name=Contoso%2A&path=%5CTeam%20A%5CCI");
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
        pipelines[0].Name.ShouldBe("Contoso PR Validation");
        pipelines[0].Path.ShouldBe("\\");
        pipelines[1].Id.ShouldBe(43);
        pipelines[1].Name.ShouldBe("Contoso Nightly");
        pipelines[1].Path.ShouldBe("\\Team A");
        pipelines[2].Id.ShouldBe(44);
        pipelines[2].Name.ShouldBe("Contoso Release");
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

    [Fact]
    public async Task ListRunsAsync_NoFilter_SendsTopApiVersionAndQueryOrderOnly()
    {
        var url = await RequestUrlForRunsAsync(new PipelineRunListQuery { Top = 50 });

        url.ShouldBe($"{RunBase}?$top=50&api-version=7.1-preview&queryOrder=queueTimeDescending");
    }

    [Fact]
    public async Task ListRunsAsync_AllFilters_SendsEveryParameterInOrder()
    {
        var url = await RequestUrlForRunsAsync(new PipelineRunListQuery
        {
            PipelineId = 42,
            Branch = "main",
            Status = "completed",
            Result = "failed",
            Reason = "pullRequest",
            Top = 10,
        });

        url.ShouldBe($"{RunBase}?$top=10&api-version=7.1-preview&queryOrder=queueTimeDescending&definitions=42&branchName=refs%2Fheads%2Fmain&statusFilter=completed&resultFilter=failed&reasonFilter=pullRequest");
    }

    [Fact]
    public async Task ListRunsAsync_BranchWithSlashAndSpace_SendsEscapedRefName()
    {
        var url = await RequestUrlForRunsAsync(new PipelineRunListQuery { Branch = "feature/a b", Top = 50 });

        url.ShouldContain("branchName=refs%2Fheads%2Ffeature%2Fa%20b");
    }

    [Fact]
    public async Task ListRunsAsync_FullRef_SendsRefVerbatim()
    {
        var url = await RequestUrlForRunsAsync(new PipelineRunListQuery { Branch = "refs/pull/815/merge", Top = 50 });

        url.ShouldContain("branchName=refs%2Fpull%2F815%2Fmerge");
    }

    [Fact]
    public async Task ListRunsAsync_EmptyStrings_SendNoFilter()
    {
        var url = await RequestUrlForRunsAsync(new PipelineRunListQuery
        {
            Branch = string.Empty,
            Status = string.Empty,
            Result = string.Empty,
            Reason = string.Empty,
            Top = 50,
        });

        url.ShouldBe($"{RunBase}?$top=50&api-version=7.1-preview&queryOrder=queueTimeDescending");
    }

    [Fact]
    public async Task ListRunsAsync_TwoRuns_MapsEveryRowInServerOrder()
    {
        // Arrange
        using var handler = new FakeHttpHandler(RunList, HttpStatusCode.OK);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsBuildClient(
            httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        // Act
        var runs = await client.ListRunsAsync(new PipelineRunListQuery { Top = 50 }, TestContext.Current.CancellationToken);

        // Assert
        runs.Count.ShouldBe(2);
        var first = runs[0];
        first.Id.ShouldBe(4201);
        first.PipelineId.ShouldBe(42);
        first.PipelineName.ShouldBe("Contoso PR Validation");
        first.BuildNumber.ShouldBe("20261106.3");
        first.Status.ShouldBe("completed");
        first.Result.ShouldBe("failed");
        first.Reason.ShouldBe("pullRequest");
        first.SourceBranch.ShouldBe("refs/pull/815/merge");
        first.SourceVersion.ShouldBe("3f2a9c1");
        first.RequestedFor.ShouldBe("Jane Doe");
        first.QueueTime.ShouldBe(DateTimeOffset.Parse("2026-11-06T07:58:01.123Z", System.Globalization.CultureInfo.InvariantCulture));
        first.StartTime.ShouldBe(DateTimeOffset.Parse("2026-11-06T07:58:20.5Z", System.Globalization.CultureInfo.InvariantCulture));
        first.FinishTime.ShouldBe(DateTimeOffset.Parse("2026-11-06T08:02:11.9Z", System.Globalization.CultureInfo.InvariantCulture));
        first.WebUrl.ShouldBe("https://devops.example.com/COL/PROJ/_build/results?buildId=4201");
        var second = runs[1];
        second.Id.ShouldBe(4200);
        second.Status.ShouldBe("inProgress");
        second.Result.ShouldBeNull();
        second.SourceBranch.ShouldBe("refs/heads/main");
    }

    [Fact]
    public async Task ListRunsAsync_EmptyValue_ReturnsEmptyList()
    {
        // Arrange
        using var handler = new FakeHttpHandler("""{"count":0,"value":[]}""", HttpStatusCode.OK);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsBuildClient(
            httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        // Act
        var runs = await client.ListRunsAsync(new PipelineRunListQuery { Top = 50 }, TestContext.Current.CancellationToken);

        // Assert
        runs.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task ListRunsAsync_TopBelowOne_ThrowsArgumentOutOfRange(int top)
    {
        // Arrange
        using var handler = new FakeHttpHandler(RunList, HttpStatusCode.OK);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsBuildClient(
            httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        // Act
        var act = () => client.ListRunsAsync(
            new PipelineRunListQuery { Top = top }, TestContext.Current.CancellationToken);

        // Assert
        await Should.ThrowAsync<ArgumentOutOfRangeException>(act);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task ListRunsAsync_PipelineIdBelowOne_ThrowsArgumentOutOfRange(int pipelineId)
    {
        // Arrange
        using var handler = new FakeHttpHandler(RunList, HttpStatusCode.OK);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsBuildClient(
            httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        // Act
        var act = () => client.ListRunsAsync(
            new PipelineRunListQuery { PipelineId = pipelineId, Top = 50 }, TestContext.Current.CancellationToken);

        // Assert
        await Should.ThrowAsync<ArgumentOutOfRangeException>(act);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Unauthorized)]
    public async Task ListRunsAsync_ErrorStatus_ThrowsHttpRequestExceptionWithStatus(HttpStatusCode status)
    {
        // Arrange
        using var handler = new FakeHttpHandler("{}", status);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsBuildClient(
            httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        // Act
        var act = () => client.ListRunsAsync(
            new PipelineRunListQuery { Top = 50 }, TestContext.Current.CancellationToken);

        // Assert
        var exception = await Should.ThrowAsync<HttpRequestException>(act);
        exception.StatusCode.ShouldBe(status);
    }

    [Fact]
    public async Task GetTimelineAsync_SendsTimelineUrlWithApiVersion()
    {
        // Arrange
        using var handler = new FakeHttpHandler(Fixtures.Read("timeline-matrix.json"), HttpStatusCode.OK);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsBuildClient(
            httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        // Act
        await client.GetTimelineAsync(4300, TestContext.Current.CancellationToken);

        // Assert
        handler.LastRequest!.Method.ShouldBe(HttpMethod.Get);
        handler.LastRequest.RequestUri!.ToString().ShouldBe(
            $"{TestConstants.ServerUrl}/{TestConstants.Collection}/{TestConstants.Project}/_apis/build/builds/4300/timeline?api-version=7.1-preview");
    }

    [Fact]
    public async Task GetTimelineAsync_RecordedMatrixTimeline_MapsEveryRecord()
    {
        // Arrange
        using var handler = new FakeHttpHandler(Fixtures.Read("timeline-matrix.json"), HttpStatusCode.OK);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsBuildClient(
            httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        // Act
        var records = await client.GetTimelineAsync(4300, TestContext.Current.CancellationToken);

        // Assert
        records.Count.ShouldBe(22);

        var install = records.Single(r => string.Equals(r.Name, "Install PowerShellGet", StringComparison.Ordinal));
        var job = records.Single(r => string.Equals(r.Type, "Job", StringComparison.Ordinal) && string.Equals(r.Name, "Verify Migrations sql2022chronological", StringComparison.Ordinal));
        install.Type.ShouldBe("Task");
        install.Identifier.ShouldBeNull();
        install.State.ShouldBe("completed");
        install.Result.ShouldBe("failed");
        install.LogId.ShouldBe(127);
        install.ErrorCount.ShouldBe(1);
        install.WarningCount.ShouldBe(0);
        install.Order.ShouldBe(8);
        install.ParentId.ShouldBe(job.Id);
        var issue = install.Issues.ShouldHaveSingleItem();
        issue.Type.ShouldBe("error");
        issue.Message.ShouldBe("PowerShell exited with code '1'.");
        issue.Line.ShouldBe(26);

        var toolsBuild = records.Single(r => string.Equals(r.Type, "Phase", StringComparison.Ordinal) && string.Equals(r.Name, "Tools build", StringComparison.Ordinal));
        toolsBuild.Identifier.ShouldBe("tools.build");
        toolsBuild.Result.ShouldBe("skipped");
        toolsBuild.LogId.ShouldBeNull();

        var checkpoint = records.First(r => string.Equals(r.Type, "Checkpoint", StringComparison.Ordinal));
        checkpoint.Order.ShouldBeNull();
        checkpoint.LogId.ShouldBeNull();

        var finalize = records.Single(r => string.Equals(r.Type, "Job", StringComparison.Ordinal) && string.Equals(r.Name, "Finalize build", StringComparison.Ordinal));
        finalize.ParentId.ShouldBeNull();
        finalize.Identifier.ShouldBeNull();
        finalize.LogId.ShouldBeNull();
        finalize.Order.ShouldBe(2147483647);
    }

    [Fact]
    public async Task GetTimelineAsync_IssueType_ComesFromTopLevelTypeNotDataType()
    {
        // Arrange
        using var handler = new FakeHttpHandler(Fixtures.Read("timeline-matrix.json"), HttpStatusCode.OK);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsBuildClient(
            httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        // Act
        var records = await client.GetTimelineAsync(4300, TestContext.Current.CancellationToken);

        // Assert
        var migrator = records.Single(r => string.Equals(r.Name, "Build database migrator", StringComparison.Ordinal));
        migrator.Issues.Count.ShouldBe(10);
        migrator.Issues.ShouldAllBe(i => string.Equals(i.Type, "warning", StringComparison.Ordinal));
        migrator.Issues[0].Line.ShouldBe(20);
        migrator.WarningCount.ShouldBe(931);
    }

    [Theory]
    [InlineData("""{"type":"error","message":"m","data":{"type":"error"}}""")]
    [InlineData("""{"type":"error","message":"m","data":{"logFileLineNumber":"abc"}}""")]
    [InlineData("""{"type":"error","message":"m"}""")]
    public async Task GetTimelineAsync_LineMissingOrNotANumber_IsNull(string issueJson)
    {
        // Arrange
        var body = $$"""{"records":[{"id":"a","type":"Task","name":"n","state":"completed","issues":[{{issueJson}}]}]}""";
        using var handler = new FakeHttpHandler(body, HttpStatusCode.OK);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsBuildClient(
            httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        // Act
        var records = await client.GetTimelineAsync(1, TestContext.Current.CancellationToken);

        // Assert
        var issue = records.ShouldHaveSingleItem().Issues.ShouldHaveSingleItem();
        issue.Line.ShouldBeNull();
        issue.Type.ShouldBe("error");
    }

    [Fact]
    public async Task GetTimelineAsync_NoContent_ReturnsEmptyList()
    {
        // Arrange
        using var handler = new FakeHttpHandler(string.Empty, HttpStatusCode.NoContent);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsBuildClient(
            httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        // Act
        var records = await client.GetTimelineAsync(1, TestContext.Current.CancellationToken);

        // Assert
        records.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetTimelineAsync_EmptyBody_ReturnsEmptyList()
    {
        // Arrange
        using var handler = new FakeHttpHandler(string.Empty, HttpStatusCode.OK);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsBuildClient(
            httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        // Act
        var records = await client.GetTimelineAsync(1, TestContext.Current.CancellationToken);

        // Assert
        records.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.Unauthorized)]
    public async Task GetTimelineAsync_ErrorStatus_ThrowsHttpRequestExceptionWithStatus(HttpStatusCode status)
    {
        // Arrange
        using var handler = new FakeHttpHandler("{}", status);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsBuildClient(
            httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        // Act
        var act = () => client.GetTimelineAsync(1, TestContext.Current.CancellationToken);

        // Assert
        var exception = await Should.ThrowAsync<HttpRequestException>(act);
        exception.StatusCode.ShouldBe(status);
    }

    [Fact]
    public async Task GetLogLinesAsync_NoRange_SendsLogUrlWithoutRangeAndAcceptsJson()
    {
        // Arrange
        using var handler = new FakeHttpHandler("""{"count":0,"value":[]}""", HttpStatusCode.OK);
        using var httpClient = CreateHttpClient(handler);
        var client = CreateClient(httpClient);

        // Act
        await client.GetLogLinesAsync(4201, 12, null, null, TestContext.Current.CancellationToken);

        // Assert
        handler.LastRequest!.Method.ShouldBe(HttpMethod.Get);
        handler.LastRequest.RequestUri!.ToString().ShouldBe(
            $"{TestConstants.ServerUrl}/{TestConstants.Collection}/{TestConstants.Project}/_apis/build/builds/4201/logs/12?api-version=7.1-preview");
        handler.LastRequest.Headers.Accept.ShouldContain(header => header.MediaType == "application/json");
    }

    [Fact]
    public async Task GetLogLinesAsync_Range_SendsStartLineAndEndLine()
    {
        var uri = await LogLinesUrlAsync(401, 600);

        uri.ShouldEndWith("?api-version=7.1-preview&startLine=401&endLine=600");
    }

    [Fact]
    public async Task GetLogLinesAsync_StartLineOnly_SendsNoEndLine()
    {
        var uri = await LogLinesUrlAsync(5, null);

        uri.ShouldEndWith("?api-version=7.1-preview&startLine=5");
    }

    [Fact]
    public async Task GetLogLinesAsync_EndLineOnly_SendsNoStartLine()
    {
        var uri = await LogLinesUrlAsync(null, 9);

        uri.ShouldEndWith("?api-version=7.1-preview&endLine=9");
    }

    [Fact]
    public async Task GetLogLinesAsync_JsonBody_ReturnsLinesInOrder()
    {
        // Arrange
        using var handler = new FakeHttpHandler(
            """{"count":3,"value":["Starting: Build","##[error]Build failed.","Finishing: Build"]}""",
            HttpStatusCode.OK);
        using var httpClient = CreateHttpClient(handler);
        var client = CreateClient(httpClient);

        // Act
        var lines = await client.GetLogLinesAsync(4201, 12, null, null, TestContext.Current.CancellationToken);

        // Assert
        lines.ShouldBe(["Starting: Build", "##[error]Build failed.", "Finishing: Build"]);
    }

    [Theory]
    [InlineData("")]
    [InlineData("""{"count":0,"value":[]}""")]
    public async Task GetLogLinesAsync_NoLines_ReturnsEmptyList(string body)
    {
        // Arrange
        using var handler = new FakeHttpHandler(body, HttpStatusCode.OK);
        using var httpClient = CreateHttpClient(handler);
        var client = CreateClient(httpClient);

        // Act
        var lines = await client.GetLogLinesAsync(4201, 12, null, null, TestContext.Current.CancellationToken);

        // Assert
        lines.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetLogLinesAsync_NoContent_ReturnsEmptyList()
    {
        // Arrange
        using var handler = new FakeHttpHandler(string.Empty, HttpStatusCode.NoContent);
        using var httpClient = CreateHttpClient(handler);
        var client = CreateClient(httpClient);

        // Act
        var lines = await client.GetLogLinesAsync(4201, 12, null, null, TestContext.Current.CancellationToken);

        // Assert
        lines.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.Unauthorized)]
    public async Task GetLogLinesAsync_ErrorStatus_ThrowsHttpRequestExceptionWithStatus(HttpStatusCode status)
    {
        // Arrange
        using var handler = new FakeHttpHandler("{}", status);
        using var httpClient = CreateHttpClient(handler);
        var client = CreateClient(httpClient);

        // Act
        var act = () => client.GetLogLinesAsync(4201, 12, null, null, TestContext.Current.CancellationToken);

        // Assert
        var exception = await Should.ThrowAsync<HttpRequestException>(act);
        exception.StatusCode.ShouldBe(status);
    }

    [Fact]
    public async Task GetLogLineCountAsync_SendsLogListUrl()
    {
        // Arrange
        using var handler = new FakeHttpHandler(LogList, HttpStatusCode.OK);
        using var httpClient = CreateHttpClient(handler);
        var client = CreateClient(httpClient);

        // Act
        await client.GetLogLineCountAsync(4201, 12, TestContext.Current.CancellationToken);

        // Assert
        handler.LastRequest!.Method.ShouldBe(HttpMethod.Get);
        handler.LastRequest.RequestUri!.ToString().ShouldBe(
            $"{TestConstants.ServerUrl}/{TestConstants.Collection}/{TestConstants.Project}/_apis/build/builds/4201/logs?api-version=7.1-preview");
    }

    [Fact]
    public async Task GetLogLineCountAsync_ReturnsLineCountOfTheMatchingLog()
    {
        // Arrange
        using var handler = new FakeHttpHandler(LogList, HttpStatusCode.OK);
        using var httpClient = CreateHttpClient(handler);
        var client = CreateClient(httpClient);

        // Act
        var count = await client.GetLogLineCountAsync(4201, 12, TestContext.Current.CancellationToken);

        // Assert
        count.ShouldBe(1001);
    }

    [Fact]
    public async Task GetLogLineCountAsync_UnknownLogId_ThrowsNotFound()
    {
        // Arrange
        using var handler = new ScriptedHttpHandler([LogList]);
        using var httpClient = CreateHttpClient(handler);
        var client = CreateClient(httpClient);

        // Act
        var act = () => client.GetLogLineCountAsync(4201, 99, TestContext.Current.CancellationToken);

        // Assert
        var exception = await Should.ThrowAsync<HttpRequestException>(act);
        exception.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        exception.Message.ShouldContain("99");
        exception.Message.ShouldContain("4201");
        handler.Requests.Count.ShouldBe(1);
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.Unauthorized)]
    public async Task GetLogLineCountAsync_ErrorStatus_ThrowsHttpRequestExceptionWithStatus(HttpStatusCode status)
    {
        // Arrange
        using var handler = new FakeHttpHandler("{}", status);
        using var httpClient = CreateHttpClient(handler);
        var client = CreateClient(httpClient);

        // Act
        var act = () => client.GetLogLineCountAsync(4201, 12, TestContext.Current.CancellationToken);

        // Assert
        var exception = await Should.ThrowAsync<HttpRequestException>(act);
        exception.StatusCode.ShouldBe(status);
    }

    private static HttpClient CreateHttpClient(HttpMessageHandler handler)
    {
        return new HttpClient(handler, disposeHandler: false) { BaseAddress = new Uri(TestConstants.ServerUrl) };
    }

    private static AzureDevOpsBuildClient CreateClient(HttpClient httpClient)
    {
        return new AzureDevOpsBuildClient(
            httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);
    }

    private static async Task<string> LogLinesUrlAsync(int? startLine, int? endLine)
    {
        using var handler = new FakeHttpHandler("""{"count":0,"value":[]}""", HttpStatusCode.OK);
        using var httpClient = CreateHttpClient(handler);
        var client = CreateClient(httpClient);

        await client.GetLogLinesAsync(4201, 12, startLine, endLine, TestContext.Current.CancellationToken);

        return handler.LastRequest!.RequestUri!.AbsoluteUri;
    }

    private static async Task<string> RequestUrlForRunsAsync(PipelineRunListQuery query)
    {
        using var handler = new FakeHttpHandler(RunList, HttpStatusCode.OK);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsBuildClient(
            httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        await client.ListRunsAsync(query, TestContext.Current.CancellationToken);

        handler.LastRequest!.Method.ShouldBe(HttpMethod.Get);
        return handler.LastRequest.RequestUri!.AbsoluteUri;
    }
}
