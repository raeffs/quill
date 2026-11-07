using Quill.Core.Actions;
using Quill.Core.Models;
using Quill.Tests.Shared.Fakes;
using Shouldly;

namespace Quill.Core.Tests.Actions;

public class PipelineTestsActionTests
{
    private static readonly TestResultQuery DefaultQuery = new() { Outcomes = ["Failed"] };

    [Fact]
    public async Task RunAsync_WithoutResults_ReadsTestRunsOnly()
    {
        var client = new ScriptedBuildClient { ListTestRuns = _ => [TestRun(7), TestRun(8)] };

        var rows = await PipelineTestsAction.RunAsync(client, new PipelineTestsRequest(4201, false, DefaultQuery), TestContext.Current.CancellationToken);

        rows.Select(r => r.Id).ShouldBe([7, 8]);
        rows.ShouldAllBe(r => r.Results == null);
        client.Calls.ShouldBe(["ListTestRuns(4201)"]);
    }

    [Fact]
    public async Task RunAsync_WithResults_ReadsResultsForEachTestRunInOrder()
    {
        var client = new ScriptedBuildClient
        {
            ListTestRuns = _ => [TestRun(7), TestRun(8)],
            ListTestResults = (id, _) => [TestResult($"t{id}")],
        };

        var rows = await PipelineTestsAction.RunAsync(client, new PipelineTestsRequest(4201, true, DefaultQuery), TestContext.Current.CancellationToken);

        rows[0].Results!.Single().Name.ShouldBe("t7");
        rows[1].Results!.Single().Name.ShouldBe("t8");
        client.Calls.ShouldBe(["ListTestRuns(4201)", "ListTestResults(7)", "ListTestResults(8)"]);
    }

    [Fact]
    public async Task RunAsync_WithResults_PassesTheQueryThrough()
    {
        TestResultQuery? seen = null;
        var client = new ScriptedBuildClient
        {
            ListTestRuns = _ => [TestRun(7)],
            ListTestResults = (_, q) =>
            {
                seen = q;
                return [];
            },
        };
        var query = new TestResultQuery { Outcomes = ["Passed"], Top = 5, Skip = 10 };

        await PipelineTestsAction.RunAsync(client, new PipelineTestsRequest(4201, true, query), TestContext.Current.CancellationToken);

        seen.ShouldBeSameAs(query);
    }

    private static TestRun TestRun(int id) => new() { Id = id, Name = "Unit", State = "Completed" };

    private static TestCaseResult TestResult(string name) => new() { Name = name, Outcome = "Failed" };
}
