using Quill.Core.Actions;
using Quill.Core.Models;
using Quill.Tests.Shared.Fakes;
using Shouldly;

namespace Quill.Core.Tests.Actions;

public class PipelineTriggerActionTests
{
    [Fact]
    public async Task RunAsync_QueuesTheRequestUnchangedAndBuildsTheRow()
    {
        PipelineTriggerRequest? queued = null;
        var client = new ScriptedBuildClient
        {
            QueueRun = request =>
            {
                queued = request;
                return Run(4201);
            },
        };
        var request = new PipelineTriggerRequest { PipelineId = 42, Branch = "main" };

        var row = await PipelineTriggerAction.RunAsync(client, request, TestContext.Current.CancellationToken);

        queued.ShouldBeSameAs(request);
        row.Id.ShouldBe(4201);
        row.Stages.ShouldBeNull();
        client.Calls.ShouldBe(["QueueRun(42)"]);
    }

    private static PipelineRun Run(int id) => new()
    {
        Id = id,
        PipelineId = 42,
        PipelineName = "Contoso CI",
        BuildNumber = "20260101.1",
        Status = "notStarted",
        Reason = "manual",
        SourceBranch = "refs/heads/main",
        SourceVersion = "0123456789abcdef",
        RequestedFor = "Jane Doe",
        QueueTime = new DateTimeOffset(2026, 1, 1, 9, 0, 0, TimeSpan.Zero),
        WebUrl = "https://dev.example.com/Contoso/_build/results?buildId=4201",
    };
}
