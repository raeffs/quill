using Quill.Core;
using Quill.Core.Models;
using Shouldly;

namespace Quill.Core.Tests;

public class PullRequestThreadResultBuilderTests
{
    [Fact]
    public void Build_CopiesScalarsAndAttachesComments()
    {
        // Arrange
        var thread = new PullRequestThread
        {
            Id = 88123,
            Status = "active",
            PublishedDate = new DateTimeOffset(2026, 5, 13, 9, 0, 0, TimeSpan.Zero),
            LastUpdatedDate = new DateTimeOffset(2026, 5, 14, 16, 30, 0, TimeSpan.Zero),
            FilePath = "src/Importer/RetryPolicy.cs",
            Side = "right",
            StartLine = 57,
            EndLine = 57,
            PositionState = "tracked",
            OrigFilePath = "src/Importer/Retry.cs",
            OrigStartLine = 40,
            OrigEndLine = 40,
            OrigStartColumn = 1,
            OrigEndColumn = null,
            Comments = Array.Empty<PullRequestComment>(),
        };
        var comments = new List<PullRequestCommentResult>
        {
            new()
            {
                Id = 1,
                Author = "John Roe",
                CreatedDate = "2026-05-13T09:00:00Z",
                LastUpdatedDate = "2026-05-13T09:00:00Z",
                UsersLiked = Array.Empty<string>(),
                Text = "Consider backoff.",
            },
        };

        // Act
        var result = PullRequestThreadResultBuilder.Build(thread, comments);

        // Assert
        result.Id.ShouldBe(88123);
        result.Status.ShouldBe("active");
        result.FilePath.ShouldBe("src/Importer/RetryPolicy.cs");
        result.Side.ShouldBe("right");
        result.StartLine.ShouldBe(57);
        result.EndLine.ShouldBe(57);
        result.PositionState.ShouldBe("tracked");
        result.OrigFilePath.ShouldBe("src/Importer/Retry.cs");
        result.OrigStartLine.ShouldBe(40);
        result.OrigEndLine.ShouldBe(40);
        result.OrigStartColumn.ShouldBe(1);
        result.OrigEndColumn.ShouldBeNull();
        result.PublishedDate.ShouldBe("2026-05-13T09:00:00Z");
        result.LastUpdatedDate.ShouldBe("2026-05-14T16:30:00Z");
        result.Comments.Count.ShouldBe(1);
        result.Comments[0].Author.ShouldBe("John Roe");
    }

    [Fact]
    public void Build_OverallThread_LeavesPositionFieldsNull()
    {
        // Arrange
        var thread = new PullRequestThread
        {
            Id = 88200,
            Status = "active",
            PublishedDate = new DateTimeOffset(2026, 5, 13, 11, 0, 0, TimeSpan.Zero),
            LastUpdatedDate = new DateTimeOffset(2026, 5, 13, 11, 0, 0, TimeSpan.Zero),
            FilePath = null,
            Side = null,
            StartLine = null,
            EndLine = null,
            Comments = Array.Empty<PullRequestComment>(),
        };

        // Act
        var result = PullRequestThreadResultBuilder.Build(thread, Array.Empty<PullRequestCommentResult>());

        // Assert
        result.FilePath.ShouldBeNull();
        result.Side.ShouldBeNull();
        result.StartLine.ShouldBeNull();
        result.EndLine.ShouldBeNull();
        result.PositionState.ShouldBeNull();
        result.OrigFilePath.ShouldBeNull();
        result.OrigStartLine.ShouldBeNull();
        result.OrigEndLine.ShouldBeNull();
        result.OrigStartColumn.ShouldBeNull();
        result.OrigEndColumn.ShouldBeNull();
        result.Comments.ShouldBeEmpty();
    }

    [Fact]
    public void BuildComment_FormatsDatesAsIsoUtcAndCopiesLikes()
    {
        // Arrange
        var comment = new PullRequestComment
        {
            Id = 7,
            Author = "Jane Doe",
            CreatedDate = new DateTimeOffset(2026, 5, 13, 11, 0, 0, TimeSpan.FromHours(2)),
            ModifiedDate = new DateTimeOffset(2026, 5, 13, 9, 40, 0, TimeSpan.Zero),
            LastUpdatedDate = new DateTimeOffset(2026, 5, 13, 11, 0, 0, TimeSpan.Zero),
            UsersLiked = ["John Roe"],
            TextHtml = "<p>ignored</p>",
        };

        // Act
        var result = PullRequestThreadResultBuilder.BuildComment(comment, "Consider backoff.");

        // Assert
        result.Id.ShouldBe(7);
        result.Author.ShouldBe("Jane Doe");
        result.CreatedDate.ShouldBe("2026-05-13T09:00:00Z");
        result.ModifiedDate.ShouldBe("2026-05-13T09:40:00Z");
        result.LastUpdatedDate.ShouldBe("2026-05-13T11:00:00Z");
        result.UsersLiked.ShouldBe(["John Roe"]);
        result.Text.ShouldBe("Consider backoff.");
    }

    [Fact]
    public void BuildComment_UneditedComment_LeavesModifiedDateNull()
    {
        // Arrange
        var comment = new PullRequestComment
        {
            Id = 8,
            Author = null,
            CreatedDate = new DateTimeOffset(2026, 5, 13, 9, 0, 0, TimeSpan.Zero),
            ModifiedDate = null,
            LastUpdatedDate = new DateTimeOffset(2026, 5, 13, 9, 0, 0, TimeSpan.Zero),
            UsersLiked = Array.Empty<string>(),
            TextHtml = string.Empty,
        };

        // Act
        var result = PullRequestThreadResultBuilder.BuildComment(comment, string.Empty);

        // Assert
        result.Author.ShouldBeNull();
        result.ModifiedDate.ShouldBeNull();
        result.UsersLiked.ShouldBeEmpty();
    }
}
