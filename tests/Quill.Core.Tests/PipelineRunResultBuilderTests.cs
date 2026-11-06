using System.Globalization;
using Quill.Core;
using Quill.Core.Models;
using Shouldly;

namespace Quill.Core.Tests;

public class PipelineRunResultBuilderTests
{
    [Fact]
    public void Build_CompletedRun_MapsEveryKey()
    {
        // Arrange
        var run = MakeRun();

        // Act
        var result = PipelineRunResultBuilder.Build(run);

        // Assert
        result.Id.ShouldBe(121549);
        result.PipelineId.ShouldBe(42);
        result.PipelineName.ShouldBe("G6 PR Validation Pipeline");
        result.BuildNumber.ShouldBe("20261106.3");
        result.Status.ShouldBe("completed");
        result.Result.ShouldBe("failed");
        result.Reason.ShouldBe("pullRequest");
        result.SourceBranch.ShouldBe("main");
        result.SourceVersion.ShouldBe("3f2a9c1");
        result.RequestedFor.ShouldBe("Raphael Fleischlin");
        result.QueueTime.ShouldBe("2026-11-06T07:58:01Z");
        result.StartTime.ShouldBe("2026-11-06T07:58:20Z");
        result.FinishTime.ShouldBe("2026-11-06T08:02:11Z");
        result.Url.ShouldBe("https://devops.example.com/COL/PROJ/_build/results?buildId=121549");
    }

    [Theory]
    [InlineData("refs/heads/main", "main")]
    [InlineData("refs/heads/feature/pipelines", "feature/pipelines")]
    [InlineData("refs/pull/23085/merge", "refs/pull/23085/merge")]
    [InlineData("refs/tags/v2.1.0", "refs/tags/v2.1.0")]
    public void Build_SourceBranch_StripsOnlyRefsHeads(string raw, string expected)
    {
        // Arrange
        var run = MakeRun(sourceBranch: raw);

        // Act
        var result = PipelineRunResultBuilder.Build(run);

        // Assert
        result.SourceBranch.ShouldBe(expected);
    }

    [Fact]
    public void Build_RunNotStarted_LeavesResultStartTimeAndFinishTimeNull()
    {
        // Arrange
        var run = new PipelineRun
        {
            Id = 121549,
            PipelineId = 42,
            PipelineName = "G6 PR Validation Pipeline",
            BuildNumber = "20261106.3",
            Status = "notStarted",
            Result = null,
            Reason = "pullRequest",
            SourceBranch = "refs/heads/main",
            SourceVersion = "3f2a9c1",
            RequestedFor = "Raphael Fleischlin",
            QueueTime = DateTimeOffset.Parse("2026-11-06T08:58:01+01:00", CultureInfo.InvariantCulture),
            StartTime = null,
            FinishTime = null,
            WebUrl = "https://devops.example.com/COL/PROJ/_build/results?buildId=121549",
        };

        // Act
        var result = PipelineRunResultBuilder.Build(run);

        // Assert
        result.Result.ShouldBeNull();
        result.StartTime.ShouldBeNull();
        result.FinishTime.ShouldBeNull();
        result.QueueTime.ShouldBe("2026-11-06T07:58:01Z");
    }

    [Theory]
    [InlineData("cancelling", "canceled", "individualCI")]
    [InlineData("completed", "partiallySucceeded", "manual")]
    public void Build_StatusResultAndReason_PassVerbatim(string status, string runResult, string reason)
    {
        // Arrange
        var run = MakeRun(status: status, result: runResult, reason: reason);

        // Act
        var result = PipelineRunResultBuilder.Build(run);

        // Assert
        result.Status.ShouldBe(status);
        result.Result.ShouldBe(runResult);
        result.Reason.ShouldBe(reason);
    }

    [Fact]
    public void Build_WithoutStages_LeavesStagesNull()
    {
        var result = PipelineRunResultBuilder.Build(MakeRun());

        result.Stages.ShouldBeNull();
    }

    [Fact]
    public void Build_WithStages_PassesThemThrough()
    {
        IReadOnlyList<PipelineStageResult> stages = [];

        var result = PipelineRunResultBuilder.Build(MakeRun(), stages);

        result.Stages.ShouldBeSameAs(stages);
    }

    private static PipelineRun MakeRun(
        string status = "completed",
        string? result = "failed",
        string reason = "pullRequest",
        string sourceBranch = "refs/heads/main")
    {
        return new PipelineRun
        {
            Id = 121549,
            PipelineId = 42,
            PipelineName = "G6 PR Validation Pipeline",
            BuildNumber = "20261106.3",
            Status = status,
            Result = result,
            Reason = reason,
            SourceBranch = sourceBranch,
            SourceVersion = "3f2a9c1",
            RequestedFor = "Raphael Fleischlin",
            QueueTime = DateTimeOffset.Parse("2026-11-06T08:58:01+01:00", CultureInfo.InvariantCulture),
            StartTime = DateTimeOffset.Parse("2026-11-06T07:58:20Z", CultureInfo.InvariantCulture),
            FinishTime = DateTimeOffset.Parse("2026-11-06T08:02:11Z", CultureInfo.InvariantCulture),
            WebUrl = "https://devops.example.com/COL/PROJ/_build/results?buildId=121549",
        };
    }
}
