using Quill.Core;
using Quill.Core.Models;
using Shouldly;

namespace Quill.Core.Tests;

public class PullRequestResultBuilderTests
{
    [Fact]
    public void Build_UserIsReviewer_PopulatesMyVoteAndMyIsRequired()
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
        result.MyVote.ShouldBe(-5);
        result.MyIsRequired.ShouldBe(true);
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
    public void Build_CopiesScalarsAndProjectsReviewers()
    {
        // Arrange
        var pr = MakePullRequest(
            [
                new PullRequestReviewer { Id = "u1", DisplayName = "John Roe", Vote = 10, IsRequired = true },
            ]);

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
        result.Reviewers.Count.ShouldBe(1);
        result.Reviewers[0].DisplayName.ShouldBe("John Roe");
        result.Reviewers[0].Vote.ShouldBe(10);
        result.Reviewers[0].IsRequired.ShouldBeTrue();
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

    private static PullRequest MakePullRequest(IReadOnlyList<PullRequestReviewer> reviewers, DateTimeOffset? closedDate = null)
    {
        // Arrange
        return new PullRequest
        {
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
