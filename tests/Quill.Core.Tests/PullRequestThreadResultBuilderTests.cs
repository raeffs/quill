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
            FilePath = "src/Importer/Retry.cs",
            Side = "right",
            StartLine = 42,
            EndLine = 42,
            Comments = Array.Empty<WorkItemComment>(),
        };
        var comments = new List<CommentResult>
        {
            new() { Id = 1, Author = "John Roe", CreatedDate = "2026-05-13T09:00:00Z", Text = "Consider backoff." },
        };

        // Act
        var result = PullRequestThreadResultBuilder.Build(thread, comments);

        // Assert
        result.Id.ShouldBe(88123);
        result.Status.ShouldBe("active");
        result.FilePath.ShouldBe("src/Importer/Retry.cs");
        result.Side.ShouldBe("right");
        result.StartLine.ShouldBe(42);
        result.EndLine.ShouldBe(42);
        result.Comments.Count.ShouldBe(1);
        result.Comments[0].Author.ShouldBe("John Roe");
    }

    [Fact]
    public void Build_OverallThread_LeavesLocationFieldsNull()
    {
        // Arrange
        var thread = new PullRequestThread
        {
            Id = 88200,
            Status = "active",
            PublishedDate = new DateTimeOffset(2026, 5, 13, 11, 0, 0, TimeSpan.Zero),
            FilePath = null,
            Side = null,
            StartLine = null,
            EndLine = null,
            Comments = Array.Empty<WorkItemComment>(),
        };

        // Act
        var result = PullRequestThreadResultBuilder.Build(thread, Array.Empty<CommentResult>());

        // Assert
        result.FilePath.ShouldBeNull();
        result.Side.ShouldBeNull();
        result.StartLine.ShouldBeNull();
        result.EndLine.ShouldBeNull();
        result.Comments.ShouldBeEmpty();
    }
}
