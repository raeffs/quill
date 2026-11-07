using Quill.Core.Actions;
using Quill.Core.Models;
using Quill.Tests.Shared.Fakes;
using Shouldly;

namespace Quill.Core.Tests.Actions;

public class PipelineWaitActionTests
{
    [Fact]
    public async Task RunAsync_RunCompletesOnTheSecondPoll_ReturnsTheCompletedRow()
    {
        var client = new ScriptedBuildClient(() => Run("inProgress"), () => Run("completed"));
        var clock = new FakeWaitClock();

        var row = await PipelineWaitAction.RunAsync(client, clock, 4201, TimeSpan.FromMinutes(10), TestContext.Current.CancellationToken);

        row.Status.ShouldBe("completed");
        client.Calls.Count.ShouldBe(2);
    }

    private static PipelineRun Run(string status) => new()
    {
        Id = 4201,
        PipelineId = 42,
        PipelineName = "Contoso CI",
        BuildNumber = "20260101.1",
        Status = status,
        Reason = "manual",
        SourceBranch = "refs/heads/main",
        SourceVersion = "0123456789abcdef",
        RequestedFor = "Jane Doe",
        QueueTime = new DateTimeOffset(2026, 1, 1, 9, 0, 0, TimeSpan.Zero),
        WebUrl = "https://dev.example.com/Contoso/_build/results?buildId=4201",
    };
}
