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
}
