using System.Net;
using Quill.AzureDevOps;
using Quill.Core.Models;
using Quill.Core.Tests.Commands;
using Quill.Tests.Shared;
using Shouldly;

namespace Quill.Core.Tests;

public class PipelineStageTreeBuilderTests
{
    [Fact]
    public async Task Build_RecordedMatrixTimeline_EmitsStagesInOrder()
    {
        var stages = PipelineStageTreeBuilder.Build(await LoadAsync("timeline-matrix.json"));

        stages.Select(s => (s.Name, s.DisplayName, s.State, s.Result)).ShouldBe(
        [
            ("affected", "Preparation Stage", "completed", "succeeded"),
            ("migrations", "Verify Migrations", "completed", "failed"),
            ("tools", "Tools Stage", "completed", "succeeded"),
            (null, "Finalize build", "completed", "succeeded"),
        ]);
    }

    [Fact]
    public async Task Build_RecordedMatrixTimeline_EmitsOneJobPerMatrixEntry()
    {
        var stages = PipelineStageTreeBuilder.Build(await LoadAsync("timeline-matrix.json"));

        var jobs = stages.Single(s => string.Equals(s.Name, "migrations", StringComparison.Ordinal)).Jobs;

        jobs.Select(j => j.Name).ShouldBe(["Verify Migrations sql2022", "Verify Migrations sql2022chronological"]);
        jobs[0].Result.ShouldBe("succeeded");
        jobs[0].LogId.ShouldBe(119);
        jobs[0].Tasks.Select(t => (t.Name, t.LogId)).ShouldBe(
        [
            ("Initialize job", (int?)19),
            ("Checkout Sources", 24),
            ("Build database migrator", 104),
        ]);
        jobs[1].Result.ShouldBe("failed");
        jobs[1].LogId.ShouldBe(143);
        jobs[1].Tasks.Select(t => (t.Name, t.LogId)).ShouldBe(
        [
            ("Initialize job", (int?)96),
            ("Checkout Sources", 97),
            ("Install PowerShellGet", 127),
        ]);
    }

    [Fact]
    public async Task Build_RecordedMatrixTimeline_FailedTaskCarriesItsIssue()
    {
        var stages = PipelineStageTreeBuilder.Build(await LoadAsync("timeline-matrix.json"));

        var task = stages.Single(s => string.Equals(s.Name, "migrations", StringComparison.Ordinal)).Jobs[1].Tasks.Single(t => string.Equals(t.Name, "Install PowerShellGet", StringComparison.Ordinal));

        task.State.ShouldBe("completed");
        task.Result.ShouldBe("failed");
        task.LogId.ShouldBe(127);
        task.ErrorCount.ShouldBe(1);
        task.WarningCount.ShouldBe(0);
        var issue = task.Issues.ShouldHaveSingleItem();
        issue.Type.ShouldBe("error");
        issue.Message.ShouldBe("PowerShell exited with code '1'.");
        issue.Line.ShouldBe(26);
    }

    [Fact]
    public async Task Build_RecordedMatrixTimeline_TaskWithManyWarningsKeepsTenIssuesAndTheFullCount()
    {
        var stages = PipelineStageTreeBuilder.Build(await LoadAsync("timeline-matrix.json"));

        var task = stages.Single(s => string.Equals(s.Name, "migrations", StringComparison.Ordinal)).Jobs[0].Tasks.Single(t => string.Equals(t.Name, "Build database migrator", StringComparison.Ordinal));

        task.WarningCount.ShouldBe(931);
        task.ErrorCount.ShouldBe(0);
        task.Issues.Count.ShouldBe(10);
        task.Issues.ShouldAllBe(i => string.Equals(i.Type, "warning", StringComparison.Ordinal));
        task.Issues.Select(i => i.Line).ShouldBe([20, 22, 24, 26, 28, 30, 32, 34, 37, 39]);
    }

    [Fact]
    public async Task Build_RecordedMatrixTimeline_SkippedOrphanPhaseIsAJobWithNoTasks()
    {
        var stages = PipelineStageTreeBuilder.Build(await LoadAsync("timeline-matrix.json"));

        var job = stages.Single(s => string.Equals(s.Name, "tools", StringComparison.Ordinal)).Jobs.ShouldHaveSingleItem();

        job.Name.ShouldBe("Tools build");
        job.State.ShouldBe("completed");
        job.Result.ShouldBe("skipped");
        job.LogId.ShouldBeNull();
        job.Tasks.ShouldBeEmpty();
    }

    [Fact]
    public async Task Build_RecordedMatrixTimeline_RootJobWithTaskIsTheLastStage()
    {
        var stages = PipelineStageTreeBuilder.Build(await LoadAsync("timeline-matrix.json"));

        var stage = stages[^1];
        stage.Name.ShouldBeNull();
        stage.DisplayName.ShouldBe("Finalize build");
        var job = stage.Jobs.ShouldHaveSingleItem();
        job.Name.ShouldBe("Finalize build");
        job.LogId.ShouldBeNull();
        var task = job.Tasks.ShouldHaveSingleItem();
        task.Name.ShouldBe("Report build status");
        task.Result.ShouldBe("succeeded");
        task.LogId.ShouldBe(157);
        task.Issues.ShouldBeEmpty();
    }

    [Fact]
    public async Task Build_RecordedMatrixTimeline_EmitsNoPhaseOrCheckpoint()
    {
        var stages = PipelineStageTreeBuilder.Build(await LoadAsync("timeline-matrix.json"));

        var jobs = stages.SelectMany(s => s.Jobs).ToList();
        jobs.ShouldNotContain(j => string.Equals(j.Name, "Checkpoint", StringComparison.Ordinal));
        jobs.SelectMany(j => j.Tasks).ShouldNotContain(t => string.Equals(t.Name, "Checkpoint", StringComparison.Ordinal));
        var job = stages.Single(s => string.Equals(s.Name, "affected", StringComparison.Ordinal)).Jobs.ShouldHaveSingleItem();
        job.Name.ShouldBe("Mark affected projects");
        job.LogId.ShouldBe(14);
    }

    [Fact]
    public async Task Build_RecordedDefaultStageTimeline_EmitsOneDefaultStage()
    {
        var stages = PipelineStageTreeBuilder.Build(await LoadAsync("timeline-default-stage.json"));

        var stage = stages.ShouldHaveSingleItem();
        stage.Name.ShouldBe("__default");
        stage.DisplayName.ShouldBe("__default");
        stage.Result.ShouldBe("failed");
        var job = stage.Jobs.ShouldHaveSingleItem();
        job.Name.ShouldBe("Test");
        job.LogId.ShouldBe(35);
        job.Tasks.Count.ShouldBe(5);
        var task = job.Tasks.Single(t => string.Equals(t.Name, "Run Integration Tests", StringComparison.Ordinal));
        task.ErrorCount.ShouldBe(2);
        task.WarningCount.ShouldBe(1);
        task.Issues.Select(i => i.Type).ShouldBe(["error", "warning", "error"]);
        task.Issues.Select(i => i.Line).ShouldBe([855, 856, 858]);
    }

    [Fact]
    public void Build_FailedOrphanPhase_IsAJobWithItsLog()
    {
        var stages = PipelineStageTreeBuilder.Build(
        [
            Record("s", "Stage", "Build stage", identifier: "build"),
            Record("p", "Phase", "Build", parentId: "s", identifier: "build.build", result: "failed", logId: 7),
        ]);

        var job = stages.ShouldHaveSingleItem().Jobs.ShouldHaveSingleItem();
        job.Name.ShouldBe("Build");
        job.Result.ShouldBe("failed");
        job.LogId.ShouldBe(7);
        job.Tasks.ShouldBeEmpty();
    }

    [Fact]
    public void Build_RootJobWithoutTasks_IsDropped()
    {
        var stages = PipelineStageTreeBuilder.Build(
        [
            Record("s", "Stage", "Build stage", identifier: "build"),
            Record("p", "Phase", "Build", parentId: "s"),
            Record("j", "Job", "Build job", parentId: "p"),
            Record("root", "Job", "Finalize build"),
        ]);

        stages.ShouldHaveSingleItem().Name.ShouldBe("build");
    }

    [Fact]
    public void Build_PendingRecords_KeepNullResult()
    {
        var stages = PipelineStageTreeBuilder.Build(
        [
            Record("s", "Stage", "Stage", identifier: "s", state: "pending"),
            Record("p", "Phase", "Phase", parentId: "s", state: "pending"),
            Record("j", "Job", "Job", parentId: "p", state: "inProgress"),
            Record("t", "Task", "Task", parentId: "j", state: "pending"),
        ]);

        var stage = stages.ShouldHaveSingleItem();
        var job = stage.Jobs.ShouldHaveSingleItem();
        var task = job.Tasks.ShouldHaveSingleItem();
        stage.State.ShouldBe("pending");
        stage.Result.ShouldBeNull();
        job.State.ShouldBe("inProgress");
        job.Result.ShouldBeNull();
        task.State.ShouldBe("pending");
        task.Result.ShouldBeNull();
    }

    [Fact]
    public void Build_NoRecords_ReturnsEmptyList()
    {
        PipelineStageTreeBuilder.Build([]).ShouldBeEmpty();
    }

    [Fact]
    public void Build_ChildrenOutOfOrder_SortByOrder()
    {
        var stages = PipelineStageTreeBuilder.Build(
        [
            Record("s2", "Stage", "Second", identifier: "second", order: 2),
            Record("s1", "Stage", "First", identifier: "first", order: 1),
            Record("p", "Phase", "Phase", parentId: "s1"),
            Record("j", "Job", "Job", parentId: "p"),
            Record("t3", "Task", "Third", parentId: "j", order: 3),
            Record("tn", "Task", "Unordered", parentId: "j"),
            Record("t1", "Task", "First", parentId: "j", order: 1),
        ]);

        stages.Select(s => s.DisplayName).ShouldBe(["First", "Second"]);
        stages[0].Jobs[0].Tasks.Select(t => t.Name).ShouldBe(["First", "Third", "Unordered"]);
    }

    [Fact]
    public void Build_RecordWithMissingParent_IsSkipped()
    {
        var stages = PipelineStageTreeBuilder.Build(
        [
            Record("t", "Task", "Lost", parentId: "missing"),
        ]);

        stages.ShouldBeEmpty();
    }

    [Fact]
    public void Build_TwoPhasesInOneStage_ListJobsPhaseByPhase()
    {
        var stages = PipelineStageTreeBuilder.Build(
        [
            Record("s", "Stage", "Stage", identifier: "s"),
            Record("p2", "Phase", "Phase two", parentId: "s", order: 2),
            Record("p1", "Phase", "Phase one", parentId: "s", order: 1),
            Record("j", "Job", "A", parentId: "p1"),
        ]);

        stages.ShouldHaveSingleItem().Jobs.Select(j => j.Name).ShouldBe(["A", "Phase two"]);
    }

    private static async Task<IReadOnlyList<TimelineRecord>> LoadAsync(string fileName)
    {
        using var handler = new FakeHttpHandler(Fixtures.Read(fileName), HttpStatusCode.OK);
        using var httpClient = new HttpClient(handler);
        var client = new AzureDevOpsBuildClient(httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);
        return await client.GetTimelineAsync(1, TestContext.Current.CancellationToken);
    }

    private static TimelineRecord Record(
        string id,
        string type,
        string name,
        string? parentId = null,
        string? identifier = null,
        string state = "completed",
        string? result = null,
        int? logId = null,
        int? order = null) => new()
        {
            Id = id,
            ParentId = parentId,
            Type = type,
            Name = name,
            Identifier = identifier,
            State = state,
            Result = result,
            LogId = logId,
            Order = order,
            Issues = [],
        };
}
