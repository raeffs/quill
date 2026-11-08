using System.Text.Json;
using Quill.Core;
using Quill.Core.Models;
using Shouldly;

namespace Quill.Core.Tests;

public class PullRequestChecksResultBuilderTests
{
    [Fact]
    public void Build_MapsThePullRequestKeys()
    {
        // Arrange
        var pullRequest = MakePullRequest(mergeStatus: "succeeded", isDraft: true);

        // Act
        var result = PullRequestChecksResultBuilder.Build(pullRequest, []);

        // Assert
        result.Id.ShouldBe(4711);
        result.Status.ShouldBe("active");
        result.MergeStatus.ShouldBe("succeeded");
        result.IsDraft.ShouldBeTrue();
    }

    [Fact]
    public void Build_NoMergeStatus_LeavesItNull()
    {
        // Act
        var result = PullRequestChecksResultBuilder.Build(MakePullRequest(), []);

        // Assert
        result.MergeStatus.ShouldBeNull();
    }

    [Fact]
    public void Build_NoEvaluations_KeepsEmptyChecks()
    {
        // Act
        var result = PullRequestChecksResultBuilder.Build(MakePullRequest(), []);

        // Assert
        result.Checks.ShouldNotBeNull();
        result.Checks.ShouldBeEmpty();
    }

    [Fact]
    public void Build_Evaluation_MapsEveryKey()
    {
        // Arrange
        var evaluation = MakeEvaluation(
            name: "Build",
            isBlocking: true,
            status: "rejected",
            startedDate: new DateTimeOffset(2026, 11, 6, 7, 58, 0, 123, TimeSpan.Zero),
            completedDate: new DateTimeOffset(2026, 11, 6, 8, 2, 11, 987, TimeSpan.Zero),
            context: Json("""{"buildId":4201}"""));

        // Act
        var check = PullRequestChecksResultBuilder.Build(MakePullRequest(), [evaluation]).Checks.Single();

        // Assert
        check.Name.ShouldBe("Build");
        check.IsBlocking.ShouldBeTrue();
        check.Status.ShouldBe("rejected");
        check.StartedDate.ShouldBe("2026-11-06T07:58:00Z");
        check.CompletedDate.ShouldBe("2026-11-06T08:02:11Z");
        check.Context!.Value.GetRawText().ShouldBe("""{"buildId":4201}""");
    }

    [Fact]
    public void Build_DatesWithOffset_ConvertToUtc()
    {
        // Arrange
        var evaluation = MakeEvaluation(startedDate: new DateTimeOffset(2026, 11, 6, 9, 58, 0, TimeSpan.FromHours(2)));

        // Act
        var check = PullRequestChecksResultBuilder.Build(MakePullRequest(), [evaluation]).Checks.Single();

        // Assert
        check.StartedDate.ShouldBe("2026-11-06T07:58:00Z");
    }

    [Fact]
    public void Build_NoDates_LeavesThemNull()
    {
        // Act
        var check = PullRequestChecksResultBuilder.Build(MakePullRequest(), [MakeEvaluation()]).Checks.Single();

        // Assert
        check.StartedDate.ShouldBeNull();
        check.CompletedDate.ShouldBeNull();
    }

    [Theory]
    [InlineData("null")]
    [InlineData("{}")]
    public void Build_Context_PassesThroughUntouched(string json)
    {
        // Arrange
        var evaluation = MakeEvaluation(context: Json(json));

        // Act
        var check = PullRequestChecksResultBuilder.Build(MakePullRequest(), [evaluation]).Checks.Single();

        // Assert
        check.Context.ShouldNotBeNull();
        check.Context.Value.GetRawText().ShouldBe(json);
    }

    [Fact]
    public void Build_NoContext_LeavesContextNull()
    {
        // Act
        var check = PullRequestChecksResultBuilder.Build(MakePullRequest(), [MakeEvaluation()]).Checks.Single();

        // Assert
        check.Context.ShouldBeNull();
    }

    [Fact]
    public void Build_StatusPassesThroughVerbatim()
    {
        // Arrange
        var evaluation = MakeEvaluation(status: "someFutureStatus");

        // Act
        var check = PullRequestChecksResultBuilder.Build(MakePullRequest(), [evaluation]).Checks.Single();

        // Assert
        check.Status.ShouldBe("someFutureStatus");
    }

    [Fact]
    public void Build_KeepsTheEvaluationOrder()
    {
        // Arrange
        var evaluations = new[]
        {
            MakeEvaluation(name: "Minimum reviewers"),
            MakeEvaluation(name: "Build"),
            MakeEvaluation(name: "Work item linking"),
        };

        // Act
        var result = PullRequestChecksResultBuilder.Build(MakePullRequest(), evaluations);

        // Assert
        result.Checks.Select(c => c.Name).ShouldBe(["Minimum reviewers", "Build", "Work item linking"]);
    }

    [Theory]
    [InlineData("completed")]
    [InlineData("abandoned")]
    public void Build_NotActive_VerdictIsNull(string status)
    {
        // Arrange
        var pullRequest = MakePullRequest(status: status, isDraft: true, mergeStatus: "conflicts");

        // Act
        var result = PullRequestChecksResultBuilder.Build(
            pullRequest,
            [MakeEvaluation(isBlocking: true, status: "rejected")]);

        // Assert
        result.Verdict.ShouldBeNull();
    }

    [Fact]
    public void Build_Draft_IsBlocked()
    {
        // Act
        var result = PullRequestChecksResultBuilder.Build(MakePullRequest(isDraft: true), []);

        // Assert
        result.Verdict.ShouldBe("blocked");
    }

    [Theory]
    [InlineData("conflicts")]
    [InlineData("failure")]
    public void Build_BlockingMergeStatus_IsBlocked(string mergeStatus)
    {
        // Act
        var result = PullRequestChecksResultBuilder.Build(MakePullRequest(mergeStatus: mergeStatus), []);

        // Assert
        result.Verdict.ShouldBe("blocked");
    }

    [Theory]
    [InlineData("rejected")]
    [InlineData("broken")]
    public void Build_BlockingCheckFailed_IsBlocked(string status)
    {
        // Act
        var result = PullRequestChecksResultBuilder.Build(
            MakePullRequest(),
            [MakeEvaluation(isBlocking: true, status: status)]);

        // Assert
        result.Verdict.ShouldBe("blocked");
    }

    [Fact]
    public void Build_MergeStatusQueued_IsWaiting()
    {
        // Act
        var result = PullRequestChecksResultBuilder.Build(MakePullRequest(mergeStatus: "queued"), []);

        // Assert
        result.Verdict.ShouldBe("waiting");
    }

    [Theory]
    [InlineData("queued")]
    [InlineData("running")]
    public void Build_BlockingCheckPending_IsWaiting(string status)
    {
        // Act
        var result = PullRequestChecksResultBuilder.Build(
            MakePullRequest(),
            [MakeEvaluation(isBlocking: true, status: status)]);

        // Assert
        result.Verdict.ShouldBe("waiting");
    }

    [Fact]
    public void Build_BlockedBeatsWaiting()
    {
        // Arrange
        var evaluations = new[]
        {
            MakeEvaluation(isBlocking: true, status: "running"),
            MakeEvaluation(isBlocking: true, status: "rejected"),
        };

        // Act
        var result = PullRequestChecksResultBuilder.Build(MakePullRequest(), evaluations);

        // Assert
        result.Verdict.ShouldBe("blocked");
    }

    [Theory]
    [InlineData("rejected")]
    [InlineData("broken")]
    [InlineData("queued")]
    [InlineData("running")]
    public void Build_NonBlockingCheck_IsIgnored(string status)
    {
        // Act
        var result = PullRequestChecksResultBuilder.Build(
            MakePullRequest(),
            [MakeEvaluation(isBlocking: false, status: status)]);

        // Assert
        result.Verdict.ShouldBe("ready");
    }

    [Fact]
    public void Build_NoChecks_IsReady()
    {
        // Act
        var result = PullRequestChecksResultBuilder.Build(MakePullRequest(), []);

        // Assert
        result.Verdict.ShouldBe("ready");
    }

    [Fact]
    public void Build_BlockingChecksApproved_IsReady()
    {
        // Arrange
        var evaluations = new[]
        {
            MakeEvaluation(name: "Build", isBlocking: true, status: "approved"),
            MakeEvaluation(name: "Minimum reviewers", isBlocking: true, status: "approved"),
        };

        // Act
        var result = PullRequestChecksResultBuilder.Build(MakePullRequest(mergeStatus: "succeeded"), evaluations);

        // Assert
        result.Verdict.ShouldBe("ready");
    }

    [Theory]
    [InlineData("rejectedByPolicy")]
    [InlineData("notSet")]
    [InlineData("succeeded")]
    public void Build_MergeStatusWithoutRule_IsReady(string mergeStatus)
    {
        // Act
        var result = PullRequestChecksResultBuilder.Build(MakePullRequest(mergeStatus: mergeStatus), []);

        // Assert
        result.Verdict.ShouldBe("ready");
    }

    [Theory]
    [InlineData("notApplicable")]
    [InlineData("someFutureStatus")]
    public void Build_BlockingCheckUnknownStatus_IsReady(string status)
    {
        // Act
        var result = PullRequestChecksResultBuilder.Build(
            MakePullRequest(),
            [MakeEvaluation(isBlocking: true, status: status)]);

        // Assert
        result.Verdict.ShouldBe("ready");
    }

    private static JsonElement Json(string json)
    {
        return JsonDocument.Parse(json).RootElement.Clone();
    }

    private static PolicyEvaluation MakeEvaluation(
        string name = "Build",
        bool isBlocking = false,
        string status = "approved",
        DateTimeOffset? startedDate = null,
        DateTimeOffset? completedDate = null,
        JsonElement? context = null)
    {
        return new PolicyEvaluation
        {
            Name = name,
            IsBlocking = isBlocking,
            Status = status,
            StartedDate = startedDate,
            CompletedDate = completedDate,
            Context = context,
        };
    }

    private static PullRequest MakePullRequest(string? mergeStatus = null, bool isDraft = false, string status = "active")
    {
        return new PullRequest
        {
            Id = 4711,
            Title = "Add the Contoso widget",
            AuthorDisplayName = "Jane Doe",
            Status = status,
            IsDraft = isDraft,
            RepoName = "Contoso.Widgets",
            SourceBranch = "feature/widget",
            TargetBranch = "main",
            CreatedDate = new DateTimeOffset(2026, 11, 5, 10, 0, 0, TimeSpan.Zero),
            Reviewers = [],
            MergeStatus = mergeStatus,
            WebUrl = "https://devops.contoso.example/Widgets/_git/Contoso.Widgets/pullrequest/4711",
        };
    }
}
