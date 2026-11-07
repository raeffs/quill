using Quill.Core.Actions.Pipelines;
using Quill.Tests.Shared.Fakes;
using Shouldly;

namespace Quill.Core.Tests.Actions.Pipelines;

public class LogActionTests
{
    [Fact]
    public async Task RunAsync_Range_ReadsTheLinesOnce()
    {
        var client = new ScriptedBuildClient { GetLogLines = (_, _, _, _) => ["a", "b"] };

        var row = await LogAction.RunAsync(client, new PipelineLogRequest(4201, 12, 5, 6, null), TestContext.Current.CancellationToken);

        row.StartLine.ShouldBe(5);
        row.EndLine.ShouldBe(6);
        client.Calls.ShouldBe(["GetLogLines(4201,12,5,6)"]);
    }

    [Fact]
    public async Task RunAsync_Tail_CountsThenReadsFromTheComputedStart()
    {
        var client = new ScriptedBuildClient
        {
            GetLogLineCount = (_, _) => 100,
            GetLogLines = (_, _, _, _) => ["x", "y", "z"],
        };

        var row = await LogAction.RunAsync(client, new PipelineLogRequest(4201, 12, null, null, 3), TestContext.Current.CancellationToken);

        row.StartLine.ShouldBe(97);
        client.Calls.ShouldBe(["GetLogLineCount(4201,12)", "GetLogLines(4201,12,97,)"]);
    }

    [Fact]
    public async Task RunAsync_TailLongerThanTheLog_StartsAtLineOne()
    {
        var client = new ScriptedBuildClient
        {
            GetLogLineCount = (_, _) => 2,
            GetLogLines = (_, _, _, _) => ["x", "y"],
        };

        var row = await LogAction.RunAsync(client, new PipelineLogRequest(4201, 12, null, null, 50), TestContext.Current.CancellationToken);

        row.StartLine.ShouldBe(1);
        client.Calls.ShouldBe(["GetLogLineCount(4201,12)", "GetLogLines(4201,12,1,)"]);
    }
}
