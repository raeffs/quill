using System.Globalization;
using Quill.Core;
using Quill.Core.Models;
using Shouldly;

namespace Quill.Core.Tests;

public class PullRequestRevisionResultBuilderTests
{
    [Fact]
    public void Build_CopiesEveryKeyOfTheRow()
    {
        // Arrange
        var revision = new PullRequestRevision
        {
            Id = 3,
            CreatedDate = DateTimeOffset.Parse("2026-08-14T08:20:54.8691025Z", CultureInfo.InvariantCulture),
            Author = "Jane Doe",
            SourceCommit = "aaaa",
            TargetCommit = "bbbb",
            CommonCommit = "cccc",
        };

        // Act
        var result = PullRequestRevisionResultBuilder.Build(revision);

        // Assert
        result.Id.ShouldBe(3);
        result.Author.ShouldBe("Jane Doe");
        result.SourceCommit.ShouldBe("aaaa");
        result.TargetCommit.ShouldBe("bbbb");
        result.CommonCommit.ShouldBe("cccc");
    }

    [Fact]
    public void Build_TruncatesCreatedDateToWholeSecondsInUtc()
    {
        // Arrange
        var revision = new PullRequestRevision
        {
            Id = 1,
            CreatedDate = DateTimeOffset.Parse("2026-08-14T10:20:54.8691025+02:00", CultureInfo.InvariantCulture),
        };

        // Act
        var result = PullRequestRevisionResultBuilder.Build(revision);

        // Assert
        result.CreatedDate.ShouldBe("2026-08-14T08:20:54Z");
    }

    [Fact]
    public void Build_UnresolvedAuthor_AuthorIsNull()
    {
        // Arrange
        var revision = new PullRequestRevision
        {
            Id = 1,
            CreatedDate = DateTimeOffset.Parse("2026-08-14T08:20:54Z", CultureInfo.InvariantCulture),
        };

        // Act
        var result = PullRequestRevisionResultBuilder.Build(revision);

        // Assert
        result.Author.ShouldBeNull();
    }
}
