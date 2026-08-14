using System.Text.Json;
using Quill.Core;
using Quill.Core.Models;
using Shouldly;

namespace Quill.Core.Tests.Commands;

public class CommentsCommandLogicTests
{
    [Fact]
    public void Build_PopulatedComment_MapsAllFields()
    {
        var comment = new WorkItemComment
        {
            Id = 9002,
            Author = "John Roe",
            CreatedDate = new DateTimeOffset(2026, 4, 11, 8, 0, 0, TimeSpan.Zero),
            ModifiedDate = new DateTimeOffset(2026, 4, 11, 10, 0, 0, TimeSpan.Zero),
            TextHtml = "ignored-here",
        };

        var result = CommentsResultBuilder.Build(comment, "Blocked on dependency.");

        result.Id.ShouldBe(9002);
        result.Author.ShouldBe("John Roe");
        result.CreatedDate.ShouldBe("2026-04-11T08:00:00Z");
        result.ModifiedDate.ShouldBe("2026-04-11T10:00:00Z");
        result.Text.ShouldBe("Blocked on dependency.");
    }

    [Fact]
    public void Build_NeverEdited_ModifiedDateIsNull()
    {
        var comment = new WorkItemComment
        {
            Id = 1,
            Author = "Jane",
            CreatedDate = new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero),
            ModifiedDate = null,
            TextHtml = string.Empty,
        };

        var result = CommentsResultBuilder.Build(comment, string.Empty);

        result.ModifiedDate.ShouldBeNull();
    }

    [Fact]
    public void Build_UnresolvedAuthor_AuthorIsNull()
    {
        var comment = new WorkItemComment
        {
            Id = 1,
            Author = null,
            CreatedDate = new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero),
            TextHtml = string.Empty,
        };

        var result = CommentsResultBuilder.Build(comment, string.Empty);

        result.Author.ShouldBeNull();
    }

    [Fact]
    public void Build_NonUtcInstant_IsSerializedAsUtcIso8601()
    {
        // 08:00 in +02:00 is 06:00 UTC.
        var comment = new WorkItemComment
        {
            Id = 1,
            Author = "a",
            CreatedDate = new DateTimeOffset(2026, 4, 11, 8, 0, 0, TimeSpan.FromHours(2)),
            TextHtml = string.Empty,
        };

        var result = CommentsResultBuilder.Build(comment, string.Empty);

        result.CreatedDate.ShouldBe("2026-04-11T06:00:00Z");
    }

    [Fact]
    public void Build_Serialized_EmitsNullsForAuthorAndModifiedDate()
    {
        var comment = new WorkItemComment
        {
            Id = 1,
            Author = null,
            CreatedDate = new DateTimeOffset(2026, 4, 11, 8, 0, 0, TimeSpan.Zero),
            ModifiedDate = null,
            TextHtml = string.Empty,
        };

        var result = CommentsResultBuilder.Build(comment, "body");
        var json = JsonSerializer.Serialize(result);

        using var doc = JsonDocument.Parse(json);
        var names = doc.RootElement.EnumerateObject().Select(p => p.Name).ToArray();
        names.ShouldBe(["id", "author", "createdDate", "modifiedDate", "text"]);
        doc.RootElement.GetProperty("author").ValueKind.ShouldBe(JsonValueKind.Null);
        doc.RootElement.GetProperty("modifiedDate").ValueKind.ShouldBe(JsonValueKind.Null);
    }
}
