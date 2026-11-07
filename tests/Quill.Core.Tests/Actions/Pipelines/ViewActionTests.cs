using Quill.Core.Actions.Pipelines;
using Quill.Core.Models;
using Quill.Tests.Shared.Fakes;
using Shouldly;

namespace Quill.Core.Tests.Actions.Pipelines;

public class ViewActionTests
{
    [Fact]
    public async Task RunAsync_WithoutStages_ReadsTheRunOnly()
    {
        var client = new ScriptedBuildClient { GetRun = id => Run(id) };

        var row = await ViewAction.RunAsync(client, 4201, false, TestContext.Current.CancellationToken);

        row.Id.ShouldBe(4201);
        row.Stages.ShouldBeNull();
        client.Calls.ShouldBe(["GetRun(4201)"]);
    }

    [Fact]
    public async Task RunAsync_WithStages_ReadsTheTimelineAndBuildsTheStageTree()
    {
        var client = new ScriptedBuildClient
        {
            GetRun = id => Run(id),
            GetTimeline = _ => Records(),
        };

        var row = await ViewAction.RunAsync(client, 4201, true, TestContext.Current.CancellationToken);

        row.Stages.ShouldNotBeNull().Count.ShouldBe(1);
        client.Calls.ShouldBe(["GetRun(4201)", "GetTimeline(4201)"]);
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

    private static IReadOnlyList<TimelineRecord> Records() =>
    [
        new()
        {
            Id = "s",
            Type = "Stage",
            Name = "Build stage",
            Identifier = "build",
            State = "completed",
            Issues = [],
        },
        new()
        {
            Id = "j",
            ParentId = "s",
            Type = "Job",
            Name = "Build",
            Identifier = "build.build",
            State = "completed",
            Issues = [],
        },
    ];
}
