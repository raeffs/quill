using System.Text.Json;
using Quill.Core.Models;
using Shouldly;

namespace Quill.Cli.Tests;

public class JsonEncodingTests
{
    [Fact]
    public void ErrorResult_DoesNotEscapeHtmlOrNonAscii()
    {
        var json = JsonSerializer.Serialize(
            new ErrorResult { Error = "<b> & \"Grüße\" — ü", Code = 1 },
            CommandHelpers.Context.ErrorResult);

        json.ShouldContain("<b>");
        json.ShouldContain("&");
        json.ShouldContain("Grüße");
        json.ShouldContain("—");
        json.ShouldNotContain("\\u");
    }

    [Fact]
    public void ErrorResult_IsNotIndented()
    {
        var json = JsonSerializer.Serialize(
            new ErrorResult { Error = "x", Code = 1 },
            CommandHelpers.Context.ErrorResult);

        json.ShouldNotContain("\n");
    }

    [Fact]
    public void SearchResult_HasNoTagsKey()
    {
        var json = JsonSerializer.Serialize(
            new List<SearchResult> { new() { Id = 1, Title = "t", State = "New", Type = "Task" } },
            CommandHelpers.Context.ListSearchResult);

        json.ShouldNotContain("tags");
    }

    [Fact]
    public void SearchResult_HasNoPredecessorsKey()
    {
        var json = JsonSerializer.Serialize(
            new List<SearchResult> { new() { Id = 1, Title = "t", State = "New", Type = "Task" } },
            CommandHelpers.Context.ListSearchResult);

        json.ShouldNotContain("predecessors");
    }

    [Fact]
    public void PullRequestResult_EmitsTheTriageRow()
    {
        var json = JsonSerializer.Serialize(
            new List<PullRequestResult>
            {
                new()
                {
                    Id = 4711,
                    Title = "Fix retry policy",
                    Author = "Jane Doe",
                    State = "active",
                    IsDraft = false,
                    Repo = "importer",
                    Url = "https://server/coll/project/_git/importer/pullrequest/4711",
                    SourceBranch = "feat/retry",
                    TargetBranch = "main",
                    CreatedDate = "2026-05-12T08:00:00Z",
                    ClosedDate = null,
                    MergeStatus = "succeeded",
                    Labels = ["needs-docs"],
                    Votes = new PullRequestVoteCountsResult
                    {
                        Approved = 1,
                        WaitingForAuthor = 0,
                        Rejected = 0,
                        NoVote = 1,
                    },
                    MyVote = "noVote",
                    MyIsRequired = true,
                },
            },
            CommandHelpers.Context.ListPullRequestResult);

        json.ShouldContain("\"mergeStatus\":\"succeeded\"");
        json.ShouldContain("\"labels\":[\"needs-docs\"]");
        json.ShouldContain("\"votes\":{\"approved\":1,\"waitingForAuthor\":0,\"rejected\":0,\"noVote\":1}");
        json.ShouldContain("\"myVote\":\"noVote\"");
        json.ShouldNotContain("\"reviewers\"");
    }

    [Fact]
    public void PullRequestResult_NoMergeStatusOrLabels_StillEmitsBothKeys()
    {
        var json = JsonSerializer.Serialize(
            new List<PullRequestResult>
            {
                new()
                {
                    Id = 4711,
                    Title = "Fix retry policy",
                    Author = "Jane Doe",
                    State = "active",
                    IsDraft = false,
                    Repo = "importer",
                    Url = "https://server/coll/project/_git/importer/pullrequest/4711",
                    SourceBranch = "feat/retry",
                    TargetBranch = "main",
                    CreatedDate = "2026-05-12T08:00:00Z",
                    ClosedDate = null,
                    MergeStatus = null,
                    Labels = [],
                    Votes = new PullRequestVoteCountsResult
                    {
                        Approved = 0,
                        WaitingForAuthor = 0,
                        Rejected = 0,
                        NoVote = 0,
                    },
                    MyVote = null,
                    MyIsRequired = null,
                },
            },
            CommandHelpers.Context.ListPullRequestResult);

        json.ShouldContain("\"mergeStatus\":null");
        json.ShouldContain("\"labels\":[]");
        json.ShouldContain("\"myVote\":null");
    }

    [Fact]
    public void PipelineResultList_EmitsABareArrayOfIdNameFolder()
    {
        var json = JsonSerializer.Serialize(
            new List<PipelineResult> { new() { Id = 42, Name = "G6 PR Validation Pipeline", Folder = "\\" } },
            CommandHelpers.Context.ListPipelineResult);

        using var document = JsonDocument.Parse(json);
        document.RootElement.ValueKind.ShouldBe(JsonValueKind.Array);
        var row = document.RootElement[0];
        row.EnumerateObject().Select(p => p.Name).ShouldBe(["id", "name", "folder"]);
        row.GetProperty("folder").GetString().ShouldBe("\\");
    }

    [Fact]
    public void PipelineResultList_Empty_EmitsEmptyArray()
    {
        var json = JsonSerializer.Serialize(new List<PipelineResult>(), CommandHelpers.Context.ListPipelineResult);

        json.ShouldBe("[]");
    }

    [Fact]
    public void PipelineRunResult_EmitsExactlyTheRunRowKeysWithNulls()
    {
        var json = JsonSerializer.Serialize(
            new PipelineRunResult
            {
                Id = 121549,
                PipelineId = 7,
                PipelineName = "CI",
                BuildNumber = "20260101.1",
                Status = "inProgress",
                Reason = "manual",
                SourceBranch = "refs/heads/main",
                SourceVersion = "abc123",
                RequestedFor = "Jane Doe",
                QueueTime = "2026-01-01T10:00:00Z",
                Url = "https://devops.example.com/COL/PROJ/_build/results?buildId=121549",
            },
            CommandHelpers.Context.PipelineRunResult);

        using var document = JsonDocument.Parse(json);
        document.RootElement.EnumerateObject().Select(p => p.Name).ShouldBe(
        [
            "id", "pipelineId", "pipelineName", "buildNumber", "status", "result", "reason",
            "sourceBranch", "sourceVersion", "requestedFor", "queueTime", "startTime", "finishTime", "url",
        ]);
        document.RootElement.GetProperty("result").ValueKind.ShouldBe(JsonValueKind.Null);
        document.RootElement.GetProperty("startTime").ValueKind.ShouldBe(JsonValueKind.Null);
        document.RootElement.GetProperty("finishTime").ValueKind.ShouldBe(JsonValueKind.Null);
    }

    [Fact]
    public void PipelineRunResult_WithoutStages_HasNoStagesKey()
    {
        var json = JsonSerializer.Serialize(MakeRunRow(null), CommandHelpers.Context.PipelineRunResult);

        using var document = JsonDocument.Parse(json);
        document.RootElement.TryGetProperty("stages", out _).ShouldBeFalse();
    }

    [Fact]
    public void PipelineRunResult_WithStages_EmitsTheTreeWithExactKeys()
    {
        var stages = new List<PipelineStageResult>
        {
            new()
            {
                DisplayName = "Build",
                State = "completed",
                Jobs =
                [
                    new PipelineJobResult
                    {
                        Name = "Compile",
                        State = "completed",
                        ErrorCount = 0,
                        WarningCount = 0,
                        Issues = [],
                        Tasks =
                        [
                            new PipelineTaskResult
                            {
                                Name = "Run script",
                                State = "completed",
                                Result = "failed",
                                LogId = 7,
                                ErrorCount = 1,
                                WarningCount = 0,
                                Issues = [new PipelineIssueResult { Type = "error", Message = "boom" }],
                            },
                        ],
                    },
                ],
            },
        };

        var json = JsonSerializer.Serialize(MakeRunRow(stages), CommandHelpers.Context.PipelineRunResult);

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        root.EnumerateObject().Select(p => p.Name).ShouldBe(
        [
            "id", "pipelineId", "pipelineName", "buildNumber", "status", "result", "reason",
            "sourceBranch", "sourceVersion", "requestedFor", "queueTime", "startTime", "finishTime", "url",
            "stages",
        ]);
        var stage = root.GetProperty("stages")[0];
        stage.EnumerateObject().Select(p => p.Name).ShouldBe(["name", "displayName", "state", "result", "jobs"]);
        stage.GetProperty("name").ValueKind.ShouldBe(JsonValueKind.Null);
        var job = stage.GetProperty("jobs")[0];
        job.EnumerateObject().Select(p => p.Name).ShouldBe(["name", "state", "result", "logId", "errorCount", "warningCount", "issues", "tasks"]);
        job.GetProperty("logId").ValueKind.ShouldBe(JsonValueKind.Null);
        var task = job.GetProperty("tasks")[0];
        task.EnumerateObject().Select(p => p.Name).ShouldBe(
            ["name", "state", "result", "logId", "errorCount", "warningCount", "issues"]);
        var issue = task.GetProperty("issues")[0];
        issue.EnumerateObject().Select(p => p.Name).ShouldBe(["type", "message", "line"]);
        issue.GetProperty("line").ValueKind.ShouldBe(JsonValueKind.Null);
        json.ShouldNotContain("parentId");
    }

    [Fact]
    public void PipelineRunResult_WithEmptyStages_EmitsEmptyArray()
    {
        var json = JsonSerializer.Serialize(MakeRunRow([]), CommandHelpers.Context.PipelineRunResult);

        using var document = JsonDocument.Parse(json);
        var stages = document.RootElement.GetProperty("stages");
        stages.ValueKind.ShouldBe(JsonValueKind.Array);
        stages.GetArrayLength().ShouldBe(0);
    }

    [Fact]
    public void PipelineRunResultList_EmitsABareArrayOfRunRows()
    {
        var json = JsonSerializer.Serialize(
            new List<PipelineRunResult>
            {
                new()
                {
                    Id = 121549,
                    PipelineId = 7,
                    PipelineName = "CI",
                    BuildNumber = "20260101.1",
                    Status = "inProgress",
                    Reason = "manual",
                    SourceBranch = "refs/heads/main",
                    SourceVersion = "abc123",
                    RequestedFor = "Jane Doe",
                    QueueTime = "2026-01-01T10:00:00Z",
                    Url = "https://devops.example.com/COL/PROJ/_build/results?buildId=121549",
                },
            },
            CommandHelpers.Context.ListPipelineRunResult);

        using var document = JsonDocument.Parse(json);
        document.RootElement.ValueKind.ShouldBe(JsonValueKind.Array);
        document.RootElement[0].EnumerateObject().Select(p => p.Name).ShouldBe(
        [
            "id", "pipelineId", "pipelineName", "buildNumber", "status", "result", "reason",
            "sourceBranch", "sourceVersion", "requestedFor", "queueTime", "startTime", "finishTime", "url",
        ]);
    }

    [Fact]
    public void PipelineRunResultList_Empty_EmitsEmptyArray()
    {
        var json = JsonSerializer.Serialize(
            new List<PipelineRunResult>(),
            CommandHelpers.Context.ListPipelineRunResult);

        json.ShouldBe("[]");
    }

    [Fact]
    public void PullRequestViewResult_NoMergeAttempt_StillEmitsBothCommitKeys()
    {
        var json = JsonSerializer.Serialize(
            MakeViewResult([]),
            CommandHelpers.Context.PullRequestViewResult);

        json.ShouldContain("\"lastMergeSourceCommit\":null");
        json.ShouldContain("\"lastMergeTargetCommit\":null");
    }

    [Fact]
    public void PullRequestViewResult_NullWorkItems_OmitsWorkItemsAndThreads()
    {
        var json = JsonSerializer.Serialize(
            MakeViewResult([], withWorkItems: false),
            CommandHelpers.Context.PullRequestViewResult);

        json.ShouldNotContain("\"workItems\"");
        json.ShouldNotContain("\"threads\"");
        json.ShouldContain("\"myVote\":null");
        json.ShouldContain("\"myIsRequired\":null");
    }

    [Fact]
    public void PullRequestViewResult_Reviewers_CarryNamedVoteAndIsContainer()
    {
        var json = JsonSerializer.Serialize(
            MakeViewResult(
                [
                    new PullRequestReviewerResult
                    {
                        DisplayName = "Importer Team",
                        Vote = "noVote",
                        IsRequired = true,
                        IsContainer = true,
                    },
                ]),
            CommandHelpers.Context.PullRequestViewResult);

        json.ShouldContain("\"vote\":\"noVote\"");
        json.ShouldContain("\"isContainer\":true");
    }

    [Fact]
    public void PullRequestThreadResult_CarriesBothPositionsAndOmitsOrigFilePath()
    {
        var json = JsonSerializer.Serialize(
            new List<PullRequestThreadResult> { MakeThreadResult(origFilePath: null) },
            CommandHelpers.Context.ListPullRequestThreadResult);

        json.ShouldContain("\"startLine\":57");
        json.ShouldContain("\"origStartLine\":40");
        json.ShouldContain("\"origStartColumn\":1");
        json.ShouldContain("\"origEndColumn\":null");
        json.ShouldContain("\"positionState\":\"tracked\"");
        json.ShouldContain("\"publishedDate\":\"2026-05-13T09:00:00Z\"");
        json.ShouldContain("\"lastUpdatedDate\":\"2026-05-14T16:30:00Z\"");
        json.ShouldContain("\"usersLiked\":[\"Jane Doe\"]");
        json.ShouldNotContain("origFilePath");
    }

    [Fact]
    public void PullRequestThreadResult_RenamedFile_EmitsOrigFilePath()
    {
        var json = JsonSerializer.Serialize(
            new List<PullRequestThreadResult> { MakeThreadResult(origFilePath: "src/Importer/Retry.cs") },
            CommandHelpers.Context.ListPullRequestThreadResult);

        json.ShouldContain("\"origFilePath\":\"src/Importer/Retry.cs\"");
    }

    [Fact]
    public void PullRequestRevisionResult_EmitsTheSixKeyRow()
    {
        var json = JsonSerializer.Serialize(
            new List<PullRequestRevisionResult>
            {
                new()
                {
                    Id = 3,
                    CreatedDate = "2026-08-14T08:20:54Z",
                    Author = "Jane Doe",
                    SourceCommit = "aaaa",
                    TargetCommit = "bbbb",
                    CommonCommit = "cccc",
                },
            },
            CommandHelpers.Context.ListPullRequestRevisionResult);

        json.ShouldBe(
            """[{"id":3,"createdDate":"2026-08-14T08:20:54Z","author":"Jane Doe","sourceCommit":"aaaa","targetCommit":"bbbb","commonCommit":"cccc"}]""");
    }

    [Fact]
    public void PullRequestRevisionResult_NeverEmitsReason()
    {
        // The server reports "push" on every revision, including rebases and force pushes.
        // Emitting it would tell an agent the history was not rewritten when it was.
        var json = JsonSerializer.Serialize(
            new List<PullRequestRevisionResult>
            {
                new() { Id = 1, CreatedDate = "2026-08-14T08:20:54Z" },
            },
            CommandHelpers.Context.ListPullRequestRevisionResult);

        json.ShouldNotContain("reason");
    }

    [Fact]
    public void CommentResult_KeepsTheWorkItemCommentShape()
    {
        var json = JsonSerializer.Serialize(
            new List<CommentResult>
            {
                new()
                {
                    Id = 1,
                    Author = "Jane Doe",
                    CreatedDate = "2026-04-11T08:00:00Z",
                    ModifiedDate = null,
                    Text = "Blocked on dependency.",
                },
            },
            CommandHelpers.Context.ListCommentResult);

        json.ShouldNotContain("usersLiked");
        json.ShouldNotContain("lastUpdatedDate");
    }

    private static PullRequestThreadResult MakeThreadResult(string? origFilePath)
    {
        return new PullRequestThreadResult
        {
            Id = 88123,
            Status = "active",
            FilePath = "src/Importer/RetryPolicy.cs",
            Side = "right",
            StartLine = 57,
            EndLine = 57,
            PositionState = "tracked",
            OrigFilePath = origFilePath,
            OrigStartLine = 40,
            OrigEndLine = 40,
            OrigStartColumn = 1,
            OrigEndColumn = null,
            PublishedDate = "2026-05-13T09:00:00Z",
            LastUpdatedDate = "2026-05-14T16:30:00Z",
            Comments =
            [
                new PullRequestCommentResult
                {
                    Id = 1,
                    Author = "John Roe",
                    CreatedDate = "2026-05-13T09:00:00Z",
                    ModifiedDate = null,
                    LastUpdatedDate = "2026-05-13T09:00:00Z",
                    UsersLiked = ["Jane Doe"],
                    Text = "Consider backoff.",
                },
            ],
        };
    }

    private static PullRequestViewResult MakeViewResult(
        IReadOnlyList<PullRequestReviewerResult> reviewers,
        bool withWorkItems = true)
    {
        return new PullRequestViewResult
        {
            Id = 4711,
            Title = "Fix retry policy",
            Author = "Jane Doe",
            State = "active",
            IsDraft = false,
            Repo = "importer",
            Url = "https://server/coll/project/_git/importer/pullrequest/4711",
            SourceBranch = "feat/retry",
            TargetBranch = "main",
            CreatedDate = "2026-05-12T08:00:00Z",
            ClosedDate = null,
            MergeStatus = null,
            LastMergeSourceCommit = null,
            LastMergeTargetCommit = null,
            Labels = [],
            Votes = new PullRequestVoteCountsResult
            {
                Approved = 0,
                WaitingForAuthor = 0,
                Rejected = 0,
                NoVote = 0,
            },
            Reviewers = reviewers,
            MyVote = null,
            MyIsRequired = null,
            Description = string.Empty,
            WorkItems = withWorkItems ? [] : null,
        };
    }

    private static PipelineRunResult MakeRunRow(IReadOnlyList<PipelineStageResult>? stages)
    {
        return new PipelineRunResult
        {
            Id = 121549,
            PipelineId = 7,
            PipelineName = "CI",
            BuildNumber = "20260101.1",
            Status = "inProgress",
            Reason = "manual",
            SourceBranch = "refs/heads/main",
            SourceVersion = "abc123",
            RequestedFor = "Jane Doe",
            QueueTime = "2026-01-01T10:00:00Z",
            Url = "https://devops.example.com/COL/PROJ/_build/results?buildId=121549",
            Stages = stages,
        };
    }
}
