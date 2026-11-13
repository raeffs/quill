using System.Globalization;
using System.Net;
using Quill.Core;
using Quill.Core.Models;
using Quill.Tests.Shared;
using Shouldly;

namespace Quill.AzureDevOps.Tests;

public class AzureDevOpsBuildClientTestRunTests
{
    private const string TestRunList = """
        {"count":2,"value":[
        {"id":9001,"name":"Contoso Unit Tests","state":"Completed","totalTests":120,"passedTests":110,"unanalyzedTests":8,"notApplicableTests":2},
        {"id":9002,"name":"Contoso Integration Tests","state":"InProgress","totalTests":10,"passedTests":4,"unanalyzedTests":1,"notApplicableTests":5}
        ]}
        """;

    private const string EmptyList = """{"count":0,"value":[]}""";

    private const string ResultList = """
        {"count":2,"value":[
        {"testCaseTitle":"Adds two numbers","automatedTestName":"Contoso.Tests.Calculator.Add","automatedTestStorage":"Contoso.Tests.dll","outcome":"Failed","errorMessage":"Expected 4 but was 5.","stackTrace":"at Contoso.Tests.Calculator.Add()","durationInMs":2.5,"failingSince":{"date":"2026-11-06T10:05:36.48Z","build":{"id":4199}}},
        {"testCaseTitle":"Subtracts two numbers","automatedTestName":"Contoso.Tests.Calculator.Subtract","automatedTestStorage":"Contoso.Tests.dll","outcome":"Passed","durationInMs":356.0}
        ]}
        """;

    private const string Base = $"{TestConstants.ServerUrl}/{TestConstants.Collection}/{TestConstants.Project}/_apis";

    [Fact]
    public async Task ListTestRunsAsync_SendsBuildUri()
    {
        using var handler = new SequenceHttpHandler((HttpStatusCode.OK, TestRunList));
        using var httpClient = CreateHttpClient(handler);
        var client = CreateClient(httpClient);

        await client.ListTestRunsAsync(4201, TestContext.Current.CancellationToken);

        handler.Requests[0].Method.ShouldBe(HttpMethod.Get);
        handler.Requests[0].RequestUri!.ToString().ShouldBe($"{Base}/test/runs?buildUri=vstfs:///Build/Build/4201&api-version=7.1-preview");
    }

    [Fact]
    public async Task ListTestRunsAsync_MapsEveryField()
    {
        using var handler = new SequenceHttpHandler((HttpStatusCode.OK, TestRunList));
        using var httpClient = CreateHttpClient(handler);
        var client = CreateClient(httpClient);

        var runs = await client.ListTestRunsAsync(4201, TestContext.Current.CancellationToken);

        runs.Count.ShouldBe(2);
        runs[0].Id.ShouldBe(9001);
        runs[0].Name.ShouldBe("Contoso Unit Tests");
        runs[0].State.ShouldBe("Completed");
        runs[0].TotalTests.ShouldBe(120);
        runs[0].PassedTests.ShouldBe(110);
        runs[0].UnanalyzedTests.ShouldBe(8);
        runs[0].NotApplicableTests.ShouldBe(2);
        runs[1].Id.ShouldBe(9002);
        runs[1].Name.ShouldBe("Contoso Integration Tests");
        runs[1].State.ShouldBe("InProgress");
        runs[1].TotalTests.ShouldBe(10);
        runs[1].PassedTests.ShouldBe(4);
        runs[1].UnanalyzedTests.ShouldBe(1);
        runs[1].NotApplicableTests.ShouldBe(5);
    }

    [Fact]
    public async Task ListTestRunsAsync_EmptyValue_ReturnsEmptyList()
    {
        using var handler = new SequenceHttpHandler((HttpStatusCode.OK, EmptyList));
        using var httpClient = CreateHttpClient(handler);
        var client = CreateClient(httpClient);

        var runs = await client.ListTestRunsAsync(4201, TestContext.Current.CancellationToken);

        runs.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, typeof(DeniedException))]
    [InlineData(HttpStatusCode.NotFound, typeof(RejectedException))]
    [InlineData(HttpStatusCode.InternalServerError, typeof(UnavailableException))]
    public async Task ListTestRunsAsync_ErrorStatus_ThrowsTheModeOfTheStatus(HttpStatusCode status, Type mode)
    {
        using var handler = new SequenceHttpHandler((status, "{}"));
        using var httpClient = CreateHttpClient(handler);
        var client = CreateClient(httpClient);

        var exception = await Should.ThrowAsync<QuillException>(
            () => client.ListTestRunsAsync(4201, TestContext.Current.CancellationToken));

        exception.ShouldBeOfType(mode);
    }

    [Fact]
    public async Task ListTestRunsAsync_ZeroRunId_Throws()
    {
        using var handler = new SequenceHttpHandler();
        using var httpClient = CreateHttpClient(handler);
        var client = CreateClient(httpClient);

        await Should.ThrowAsync<ArgumentOutOfRangeException>(
            () => client.ListTestRunsAsync(0, TestContext.Current.CancellationToken));

        handler.Requests.ShouldBeEmpty();
    }

    [Fact]
    public async Task ListTestResultsAsync_OneOutcome_SendsOutcomesOnly()
    {
        using var handler = new SequenceHttpHandler((HttpStatusCode.OK, ResultList));
        using var httpClient = CreateHttpClient(handler);
        var client = CreateClient(httpClient);

        await client.ListTestResultsAsync(9001, new TestResultQuery { Outcomes = ["Failed"] }, TestContext.Current.CancellationToken);

        handler.Requests[0].Method.ShouldBe(HttpMethod.Get);
        handler.Requests[0].RequestUri!.ToString().ShouldBe($"{Base}/test/Runs/9001/results?outcomes=Failed&api-version=7.1-preview");
    }

    [Fact]
    public async Task ListTestResultsAsync_OutcomesTopAndSkip_SendsAllInOrder()
    {
        using var handler = new SequenceHttpHandler((HttpStatusCode.OK, ResultList));
        using var httpClient = CreateHttpClient(handler);
        var client = CreateClient(httpClient);

        await client.ListTestResultsAsync(
            9001,
            new TestResultQuery { Outcomes = ["Passed", "NotExecuted"], Top = 100, Skip = 200 },
            TestContext.Current.CancellationToken);

        handler.Requests[0].RequestUri!.ToString().ShouldBe(
            $"{Base}/test/Runs/9001/results?outcomes=Passed,NotExecuted&$top=100&$skip=200&api-version=7.1-preview");
    }

    [Fact]
    public async Task ListTestResultsAsync_SkipZero_SendsSkipZero()
    {
        using var handler = new SequenceHttpHandler((HttpStatusCode.OK, ResultList));
        using var httpClient = CreateHttpClient(handler);
        var client = CreateClient(httpClient);

        await client.ListTestResultsAsync(
            9001,
            new TestResultQuery { Outcomes = ["Failed"], Skip = 0 },
            TestContext.Current.CancellationToken);

        handler.Requests[0].RequestUri!.ToString().ShouldContain("&$skip=0&");
    }

    [Fact]
    public async Task ListTestResultsAsync_OutcomeWithSpace_EscapesIt()
    {
        using var handler = new SequenceHttpHandler((HttpStatusCode.OK, ResultList));
        using var httpClient = CreateHttpClient(handler);
        var client = CreateClient(httpClient);

        await client.ListTestResultsAsync(
            9001,
            new TestResultQuery { Outcomes = ["Not Executed"] },
            TestContext.Current.CancellationToken);

        handler.Requests[0].RequestUri!.AbsoluteUri.ShouldContain("outcomes=Not%20Executed&");
    }

    [Fact]
    public async Task ListTestResultsAsync_FailedResult_MapsEveryField()
    {
        using var handler = new SequenceHttpHandler((HttpStatusCode.OK, ResultList));
        using var httpClient = CreateHttpClient(handler);
        var client = CreateClient(httpClient);

        var results = await client.ListTestResultsAsync(
            9001,
            new TestResultQuery { Outcomes = ["Failed"] },
            TestContext.Current.CancellationToken);

        results.Count.ShouldBe(2);
        var failed = results[0];
        failed.Title.ShouldBe("Adds two numbers");
        failed.Name.ShouldBe("Contoso.Tests.Calculator.Add");
        failed.Storage.ShouldBe("Contoso.Tests.dll");
        failed.Outcome.ShouldBe("Failed");
        failed.Message.ShouldBe("Expected 4 but was 5.");
        failed.StackTrace.ShouldBe("at Contoso.Tests.Calculator.Add()");
        failed.DurationInMs.ShouldBe(2.5);
        failed.FailingSince.ShouldBe(DateTimeOffset.Parse("2026-11-06T10:05:36.48Z", CultureInfo.InvariantCulture));
        failed.FailingSinceRunId.ShouldBe(4199);
    }

    [Fact]
    public async Task ListTestResultsAsync_PassedResult_KeepsDurationAndLeavesFailureFieldsNull()
    {
        using var handler = new SequenceHttpHandler((HttpStatusCode.OK, ResultList));
        using var httpClient = CreateHttpClient(handler);
        var client = CreateClient(httpClient);

        var results = await client.ListTestResultsAsync(
            9001,
            new TestResultQuery { Outcomes = ["Passed"] },
            TestContext.Current.CancellationToken);

        var passed = results[1];
        passed.Title.ShouldBe("Subtracts two numbers");
        passed.Outcome.ShouldBe("Passed");
        passed.Message.ShouldBeNull();
        passed.StackTrace.ShouldBeNull();
        passed.DurationInMs.ShouldBe(356.0);
        passed.FailingSince.ShouldBeNull();
        passed.FailingSinceRunId.ShouldBeNull();
    }

    [Fact]
    public async Task ListTestResultsAsync_FailingSinceWithoutBuild_LeavesRunIdNull()
    {
        const string body = """{"count":1,"value":[{"outcome":"Failed","failingSince":{"date":"2026-11-06T10:05:36Z"}}]}""";
        using var handler = new SequenceHttpHandler((HttpStatusCode.OK, body));
        using var httpClient = CreateHttpClient(handler);
        var client = CreateClient(httpClient);

        var results = await client.ListTestResultsAsync(
            9001,
            new TestResultQuery { Outcomes = ["Failed"] },
            TestContext.Current.CancellationToken);

        results[0].FailingSince.ShouldBe(DateTimeOffset.Parse("2026-11-06T10:05:36Z", CultureInfo.InvariantCulture));
        results[0].FailingSinceRunId.ShouldBeNull();
    }

    [Fact]
    public async Task ListTestResultsAsync_BadRequest_ThrowsRejected()
    {
        using var handler = new SequenceHttpHandler((HttpStatusCode.BadRequest, """{"message":"Invalid outcome."}"""));
        using var httpClient = CreateHttpClient(handler);
        var client = CreateClient(httpClient);

        var exception = await Should.ThrowAsync<RejectedException>(
            () => client.ListTestResultsAsync(
                9001,
                new TestResultQuery { Outcomes = ["Bogus"] },
                TestContext.Current.CancellationToken));

        exception.Message.ShouldBe("Invalid outcome. (400)");
    }

    [Theory]
    [InlineData(0, 1, 0)]
    [InlineData(9001, 0, 0)]
    [InlineData(9001, 1, -1)]
    public async Task ListTestResultsAsync_BadArguments_Throw(int testRunId, int top, int skip)
    {
        using var handler = new SequenceHttpHandler();
        using var httpClient = CreateHttpClient(handler);
        var client = CreateClient(httpClient);

        await Should.ThrowAsync<ArgumentOutOfRangeException>(
            () => client.ListTestResultsAsync(
                testRunId,
                new TestResultQuery { Outcomes = ["Failed"], Top = top, Skip = skip },
                TestContext.Current.CancellationToken));

        handler.Requests.ShouldBeEmpty();
    }

    [Fact]
    public async Task ListTestResultsAsync_NoOutcomes_Throws()
    {
        using var handler = new SequenceHttpHandler();
        using var httpClient = CreateHttpClient(handler);
        var client = CreateClient(httpClient);

        await Should.ThrowAsync<ArgumentOutOfRangeException>(
            () => client.ListTestResultsAsync(
                9001,
                new TestResultQuery { Outcomes = [] },
                TestContext.Current.CancellationToken));

        handler.Requests.ShouldBeEmpty();
    }

    private static HttpClient CreateHttpClient(SequenceHttpHandler handler) =>
        TestHttp.Client(handler);

    private static AzureDevOpsBuildClient CreateClient(HttpClient httpClient) =>
        new(httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);
}
