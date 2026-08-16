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
    public void PullRequestViewResult_NoMergeAttempt_StillEmitsBothCommitKeys()
    {
        var json = JsonSerializer.Serialize(
            MakeViewResult([]),
            CommandHelpers.Context.PullRequestViewResult);

        json.ShouldContain("\"lastMergeSourceCommit\":null");
        json.ShouldContain("\"lastMergeTargetCommit\":null");
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

    private static PullRequestViewResult MakeViewResult(IReadOnlyList<PullRequestReviewerResult> reviewers)
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
            WorkItems = [],
        };
    }
}
