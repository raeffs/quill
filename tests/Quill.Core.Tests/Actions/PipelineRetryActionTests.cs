using Quill.Core.Actions;
using Quill.Core.Models;
using Quill.Tests.Shared.Fakes;
using Shouldly;

namespace Quill.Core.Tests.Actions;

public class PipelineRetryActionTests
{
    [Fact]
    public async Task RunAsync_RetriesTheRunAndBuildsTheRow()
    {
        var client = new ScriptedBuildClient { RetryRun = id => Run(id) };

        var row = await PipelineRetryAction.RunAsync(client, 4201, TestContext.Current.CancellationToken);

        row.Id.ShouldBe(4201);
        row.Stages.ShouldBeNull();
        client.Calls.ShouldBe(["RetryRun(4201)"]);
    }

    private static PipelineRun Run(int id) => new()
    {
        Id = id,
        PipelineId = 42,
        PipelineName = "Contoso CI",
        BuildNumber = "20260101.1",
        Status = "inProgress",
        Reason = "manual",
        SourceBranch = "refs/heads/main",
        SourceVersion = "0123456789abcdef",
        RequestedFor = "Jane Doe",
        QueueTime = new DateTimeOffset(2026, 1, 1, 9, 0, 0, TimeSpan.Zero),
        WebUrl = "https://dev.example.com/Contoso/_build/results?buildId=4201",
    };
}
