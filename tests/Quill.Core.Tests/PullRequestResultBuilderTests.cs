using Quill.Core;
using Quill.Core.Models;
using Shouldly;

namespace Quill.Core.Tests;

public class PullRequestResultBuilderTests
{
    [Fact]
    public void Build_UserIsReviewer_NamesMyVoteAndPopulatesMyIsRequired()
    {
        // Arrange
        var pr = MakePullRequest(
            [
                new PullRequestReviewer { Id = "u1", DisplayName = "John", Vote = 5, IsRequired = false },
                new PullRequestReviewer { Id = "me", DisplayName = "Me", Vote = -5, IsRequired = true },
            ]);

        // Act
        var result = PullRequestResultBuilder.Build(pr, currentUserId: "me");

        // Assert
        result.MyVote.ShouldBe("waitingForAuthor");
        result.MyIsRequired.ShouldBe(true);
    }

    [Theory]
    [InlineData(10, "approved")]
    [InlineData(5, "approvedWithSuggestions")]
    [InlineData(0, "noVote")]
    [InlineData(-5, "waitingForAuthor")]
    [InlineData(-10, "rejected")]
    public void Build_UserIsReviewer_NamesEveryVote(int vote, string expected)
    {
        // Arrange
        var pr = MakePullRequest(
            [
                new PullRequestReviewer { Id = "me", DisplayName = "Me", Vote = vote, IsRequired = true },
            ]);

        // Act
        var result = PullRequestResultBuilder.Build(pr, currentUserId: "me");

        // Assert
        result.MyVote.ShouldBe(expected);
    }

    [Fact]
    public void Build_UserIsNotReviewer_LeavesMyVoteAndMyIsRequiredNull()
    {
        // Arrange
        var pr = MakePullRequest(
            [
                new PullRequestReviewer { Id = "u1", DisplayName = "John", Vote = 0, IsRequired = false },
            ]);

        // Act
        var result = PullRequestResultBuilder.Build(pr, currentUserId: "me");

        // Assert
        result.MyVote.ShouldBeNull();
        result.MyIsRequired.ShouldBeNull();
    }

    [Fact]
    public void Build_CopiesScalars()
    {
        // Arrange
        var pr = MakePullRequest(
            [
                new PullRequestReviewer { Id = "u1", DisplayName = "John Roe", Vote = 10, IsRequired = true },
            ],
            mergeStatus: "succeeded",
            labels: ["needs-docs"]);

        // Act
        var result = PullRequestResultBuilder.Build(pr, currentUserId: "me");

        // Assert
        result.Id.ShouldBe(42);
        result.Title.ShouldBe("Fix it");
        result.Author.ShouldBe("Jane Doe");
        result.State.ShouldBe("active");
        result.IsDraft.ShouldBeFalse();
        result.Repo.ShouldBe("importer");
        result.Url.ShouldBe("https://server/coll/project/_git/importer/pullrequest/42");
        result.SourceBranch.ShouldBe("feat/x");
        result.TargetBranch.ShouldBe("main");
        result.CreatedDate.ShouldBe("2026-05-12T08:00:00Z");
        result.ClosedDate.ShouldBeNull();
        result.MergeStatus.ShouldBe("succeeded");
        result.Labels.ShouldBe(["needs-docs"]);
    }

    [Fact]
    public void Build_NoLabels_EmitsEmptyArray()
    {
        // Arrange
        var pr = MakePullRequest([]);

        // Act
        var result = PullRequestResultBuilder.Build(pr, currentUserId: "me");

        // Assert
        result.Labels.ShouldBeEmpty();
    }

    [Fact]
    public void Build_MixedVotes_CountsThemFoldingApprovedWithSuggestions()
    {
        // Arrange
        var pr = MakePullRequest(
            [
                new PullRequestReviewer { Id = "u1", DisplayName = "Approver", Vote = 10, IsRequired = true },
                new PullRequestReviewer { Id = "u2", DisplayName = "Suggester", Vote = 5, IsRequired = false },
                new PullRequestReviewer { Id = "u3", DisplayName = "Waiter", Vote = -5, IsRequired = true },
                new PullRequestReviewer { Id = "u4", DisplayName = "Rejecter", Vote = -10, IsRequired = true },
                new PullRequestReviewer { Id = "u5", DisplayName = "Silent", Vote = 0, IsRequired = false },
            ]);

        // Act
        var result = PullRequestResultBuilder.Build(pr, currentUserId: "me");

        // Assert
        result.Votes.Approved.ShouldBe(2);
        result.Votes.WaitingForAuthor.ShouldBe(1);
        result.Votes.Rejected.ShouldBe(1);
        result.Votes.NoVote.ShouldBe(1);
    }

    [Fact]
    public void Build_ContainerReviewer_IsNotCounted()
    {
        // Arrange
        var pr = MakePullRequest(
            [
                new PullRequestReviewer { Id = "g1", DisplayName = "Team", Vote = 0, IsRequired = true, IsContainer = true },
                new PullRequestReviewer { Id = "u1", DisplayName = "Person", Vote = 0, IsRequired = false },
            ]);

        // Act
        var result = PullRequestResultBuilder.Build(pr, currentUserId: "me");

        // Assert
        result.Votes.NoVote.ShouldBe(1);
    }

    [Fact]
    public void Build_ClosedDateSet_FormatsAsIsoUtc()
    {
        // Arrange
        var closed = new DateTimeOffset(2026, 6, 1, 12, 30, 0, TimeSpan.Zero);
        var pr = MakePullRequest(
            [],
            closedDate: closed);

        // Act
        var result = PullRequestResultBuilder.Build(pr, currentUserId: "me");

        // Assert
        result.ClosedDate.ShouldBe("2026-06-01T12:30:00Z");
    }

    private static PullRequest MakePullRequest(
        IReadOnlyList<PullRequestReviewer> reviewers,
        DateTimeOffset? closedDate = null,
        string? mergeStatus = null,
        IReadOnlyList<string>? labels = null)
    {
        // Arrange
        return new PullRequest
        {
            MergeStatus = mergeStatus,
            Labels = labels ?? [],
            Id = 42,
            Title = "Fix it",
            AuthorDisplayName = "Jane Doe",
            Status = "active",
            IsDraft = false,
            RepoName = "importer",
            SourceBranch = "feat/x",
            TargetBranch = "main",
            CreatedDate = new DateTimeOffset(2026, 5, 12, 8, 0, 0, TimeSpan.Zero),
            ClosedDate = closedDate,
            Reviewers = reviewers,
            WebUrl = "https://server/coll/project/_git/importer/pullrequest/42",
        };
    }
}
