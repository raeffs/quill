using Quill.Core;
using Quill.Core.Models;
using Shouldly;

namespace Quill.Core.Tests;

public class PullRequestViewResultBuilderTests
{
    [Fact]
    public void Build_BaseShape_CopiesAllScalarsAndDescription()
    {
        // Arrange
        var pr = MakePullRequest([], description: "<p>hi</p>");

        // Act
        var result = PullRequestViewResultBuilder.Build(
            pr,
            currentUserId: "me",
            markdownDescription: "hi",
            workItems: Array.Empty<PullRequestLinkedWorkItemResult>(),
            threads: null,
            diffStats: null);

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
        result.Description.ShouldBe("hi");
        result.WorkItems.ShouldBeEmpty();
        result.Threads.ShouldBeNull();
        result.DiffStats.ShouldBeNull();
    }

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
        var result = PullRequestViewResultBuilder.Build(
            pr,
            currentUserId: "me",
            markdownDescription: string.Empty,
            workItems: Array.Empty<PullRequestLinkedWorkItemResult>(),
            threads: null,
            diffStats: null);

        // Assert
        result.MyVote.ShouldBe("waitingForAuthor");
        result.MyIsRequired.ShouldBe(true);
    }

    [Fact]
    public void Build_EmitsEveryTriageKeyAndKeepsReviewers()
    {
        // Arrange
        var pr = MakePullRequest(
            [
                new PullRequestReviewer { Id = "u1", DisplayName = "John Roe", Vote = 10, IsRequired = true },
                new PullRequestReviewer { Id = "g1", DisplayName = "Team", Vote = 0, IsRequired = true, IsContainer = true },
            ],
            mergeStatus: "conflicts",
            labels: ["needs-docs"]);

        // Act
        var result = PullRequestViewResultBuilder.Build(
            pr,
            currentUserId: "me",
            markdownDescription: string.Empty,
            workItems: Array.Empty<PullRequestLinkedWorkItemResult>(),
            threads: null,
            diffStats: null);

        // Assert
        result.MergeStatus.ShouldBe("conflicts");
        result.Labels.ShouldBe(["needs-docs"]);
        result.Votes.Approved.ShouldBe(1);
        result.Votes.NoVote.ShouldBe(0);
        result.Reviewers.Count.ShouldBe(2);
        result.Reviewers[0].DisplayName.ShouldBe("John Roe");
    }

    [Fact]
    public void Build_NamesEveryReviewerVote()
    {
        // Arrange
        var pr = MakePullRequest(
            [
                new PullRequestReviewer { Id = "u1", DisplayName = "Approver", Vote = 10, IsRequired = false },
                new PullRequestReviewer { Id = "u2", DisplayName = "Suggester", Vote = 5, IsRequired = false },
                new PullRequestReviewer { Id = "u3", DisplayName = "Waiter", Vote = -5, IsRequired = false },
                new PullRequestReviewer { Id = "u4", DisplayName = "Rejecter", Vote = -10, IsRequired = false },
                new PullRequestReviewer { Id = "u5", DisplayName = "Silent", Vote = 0, IsRequired = false },
            ]);

        // Act
        var result = PullRequestViewResultBuilder.Build(
            pr,
            currentUserId: "me",
            markdownDescription: string.Empty,
            workItems: Array.Empty<PullRequestLinkedWorkItemResult>(),
            threads: null,
            diffStats: null);

        // Assert
        result.Reviewers.Select(r => r.Vote).ShouldBe(
            ["approved", "approvedWithSuggestions", "waitingForAuthor", "rejected", "noVote"]);
    }

    [Fact]
    public void Build_ContainerReviewer_IsMarkedAndStillExcludedFromVotes()
    {
        // Arrange
        var pr = MakePullRequest(
            [
                new PullRequestReviewer { Id = "g1", DisplayName = "Importer Team", Vote = 0, IsRequired = true, IsContainer = true },
                new PullRequestReviewer { Id = "u1", DisplayName = "John Roe", Vote = 0, IsRequired = true },
            ]);

        // Act
        var result = PullRequestViewResultBuilder.Build(
            pr,
            currentUserId: "me",
            markdownDescription: string.Empty,
            workItems: Array.Empty<PullRequestLinkedWorkItemResult>(),
            threads: null,
            diffStats: null);

        // Assert
        result.Reviewers.Count.ShouldBe(2);
        result.Reviewers[0].IsContainer.ShouldBeTrue();
        result.Reviewers[1].IsContainer.ShouldBeFalse();
        result.Votes.NoVote.ShouldBe(1);
    }

    [Fact]
    public void Build_CarriesTheMergeAttemptCommits()
    {
        // Arrange
        var pr = MakePullRequest(
            [],
            lastMergeSourceCommit: "1111111111111111111111111111111111111111",
            lastMergeTargetCommit: "2222222222222222222222222222222222222222");

        // Act
        var result = PullRequestViewResultBuilder.Build(
            pr,
            currentUserId: "me",
            markdownDescription: string.Empty,
            workItems: Array.Empty<PullRequestLinkedWorkItemResult>(),
            threads: null,
            diffStats: null);

        // Assert
        result.LastMergeSourceCommit.ShouldBe("1111111111111111111111111111111111111111");
        result.LastMergeTargetCommit.ShouldBe("2222222222222222222222222222222222222222");
    }

    [Fact]
    public void Build_UserNotReviewer_LeavesMyVoteAndMyIsRequiredNull()
    {
        // Arrange
        var pr = MakePullRequest([new PullRequestReviewer { Id = "u1", DisplayName = "John", Vote = 0, IsRequired = false }]);

        // Act
        var result = PullRequestViewResultBuilder.Build(
            pr,
            currentUserId: "me",
            markdownDescription: string.Empty,
            workItems: Array.Empty<PullRequestLinkedWorkItemResult>(),
            threads: null,
            diffStats: null);

        // Assert
        result.MyVote.ShouldBeNull();
        result.MyIsRequired.ShouldBeNull();
    }

    [Fact]
    public void Build_WithThreads_AttachesThreads()
    {
        // Arrange
        var pr = MakePullRequest([]);
        var threads = new[]
        {
            new PullRequestThreadResult
            {
                Id = 1, Status = "active", FilePath = null, Side = null, StartLine = null, EndLine = null,
                PublishedDate = "2026-05-13T09:00:00Z", LastUpdatedDate = "2026-05-13T09:00:00Z",
                Comments = Array.Empty<PullRequestCommentResult>(),
            },
        };

        // Act
        var result = PullRequestViewResultBuilder.Build(
            pr,
            currentUserId: "me",
            markdownDescription: string.Empty,
            workItems: Array.Empty<PullRequestLinkedWorkItemResult>(),
            threads: threads,
            diffStats: null);

        // Assert
        result.Threads.ShouldNotBeNull();
        result.Threads.ShouldHaveSingleItem().Id.ShouldBe(1);
        result.DiffStats.ShouldBeNull();
    }

    [Fact]
    public void Build_WithDiffStats_MapsAggregateAndFiles()
    {
        // Arrange
        var pr = MakePullRequest([]);
        var stats = new PullRequestDiffStats
        {
            TotalFiles = 2,
            TotalAdded = 12,
            TotalRemoved = 3,
            Files =
            [
                new PullRequestDiffFile { Path = "src/Foo.cs", ChangeType = "edit", Added = 12, Removed = 3 },
                new PullRequestDiffFile { Path = "src/Bar.cs", ChangeType = "rename", OldPath = "src/Baz.cs", Added = 0, Removed = 0 },
            ],
        };

        // Act
        var result = PullRequestViewResultBuilder.Build(
            pr,
            currentUserId: "me",
            markdownDescription: string.Empty,
            workItems: Array.Empty<PullRequestLinkedWorkItemResult>(),
            threads: null,
            diffStats: stats);

        // Assert
        result.DiffStats.ShouldNotBeNull();
        result.DiffStats!.TotalFiles.ShouldBe(2);
        result.DiffStats.TotalAdded.ShouldBe(12);
        result.DiffStats.TotalRemoved.ShouldBe(3);
        result.DiffStats.Files.Count.ShouldBe(2);
        result.DiffStats.Files[1].ChangeType.ShouldBe("rename");
        result.DiffStats.Files[1].OldPath.ShouldBe("src/Baz.cs");
    }

    [Fact]
    public void BuildLinkedWorkItem_MapsAllFields()
    {
        // Arrange
        var wi = new WorkItem
        {
            Id = 12345,
            Type = "Product Backlog Item",
            Title = "Importer reliability",
            State = "Active",
            AssignedToId = "u1",
            AssignedToDisplayName = "Jane Doe",
            ParentId = 999,
        };

        // Act
        var result = PullRequestViewResultBuilder.BuildLinkedWorkItem(wi);

        // Assert
        result.Id.ShouldBe(12345);
        result.Title.ShouldBe("Importer reliability");
        result.State.ShouldBe("Active");
        result.Type.ShouldBe("Product Backlog Item");
        result.AssignedTo.ShouldBe("Jane Doe");
        result.ParentId.ShouldBe(999);
        result.Error.ShouldBeNull();
    }

    [Fact]
    public void BuildLinkedWorkItem_NoAssignee_LeavesAssignedToNull()
    {
        // Arrange
        var wi = new WorkItem
        {
            Id = 12345,
            Type = "Task",
            Title = "x",
            State = "New",
            AssignedToId = string.Empty,
            AssignedToDisplayName = string.Empty,
        };

        // Act
        var result = PullRequestViewResultBuilder.BuildLinkedWorkItem(wi);

        // Assert
        result.AssignedTo.ShouldBeNull();
        result.ParentId.ShouldBeNull();
    }

    [Fact]
    public void BuildErrorStub_OnlyIdAndError()
    {
        // Arrange + Act
        var stub = PullRequestViewResultBuilder.BuildErrorStub(99999, "unreadable");

        // Assert
        stub.Id.ShouldBe(99999);
        stub.Error.ShouldBe("unreadable");
        stub.Title.ShouldBeNull();
        stub.State.ShouldBeNull();
        stub.Type.ShouldBeNull();
        stub.AssignedTo.ShouldBeNull();
        stub.ParentId.ShouldBeNull();
    }

    private static PullRequest MakePullRequest(
        IReadOnlyList<PullRequestReviewer> reviewers,
        string description = "",
        string? mergeStatus = null,
        IReadOnlyList<string>? labels = null,
        string? lastMergeSourceCommit = null,
        string? lastMergeTargetCommit = null)
    {
        return new PullRequest
        {
            MergeStatus = mergeStatus,
            LastMergeSourceCommit = lastMergeSourceCommit,
            LastMergeTargetCommit = lastMergeTargetCommit,
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
            ClosedDate = null,
            Reviewers = reviewers,
            WebUrl = "https://server/coll/project/_git/importer/pullrequest/42",
            Description = description,
        };
    }
}
