using Quill.Core.Actions;
using Quill.Core.Models;
using Quill.Tests.Shared.Fakes;
using Shouldly;

namespace Quill.Core.Tests.Actions;

public class PipelineRunsActionTests
{
    [Fact]
    public async Task RunAsync_ListsRunsAndBuildsOneRowEachWithoutStages()
    {
        var client = new ScriptedBuildClient
        {
            ListRuns = _ => [Run(4201), Run(4202)],
        };
        var query = new PipelineRunListQuery { Top = 50 };

        var rows = await PipelineRunsAction.RunAsync(client, query, TestContext.Current.CancellationToken);

        rows.Select(row => row.Id).ShouldBe([4201, 4202]);
        rows.ShouldAllBe(row => row.Stages == null);
        client.Calls.ShouldBe(["ListRuns"]);
    }

    private static PipelineRun Run(int id) => new()
    {
        Id = id,
        PipelineId = 12,
        PipelineName = "Contoso CI",
        BuildNumber = "20260101.1",
        Status = "completed",
        Result = "succeeded",
        Reason = "manual",
        SourceBranch = "refs/heads/main",
        SourceVersion = "0123456789abcdef",
        RequestedFor = "Jane Doe",
        QueueTime = new DateTimeOffset(2026, 1, 1, 9, 0, 0, TimeSpan.Zero),
        WebUrl = "https://dev.example.com/Contoso/_build/results?buildId=4201",
    };
}
