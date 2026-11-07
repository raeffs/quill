using Microsoft.Extensions.Logging.Abstractions;
using Quill.Core.Markdown;
using Quill.Core.Models;
using Shouldly;

namespace Quill.Core.Tests;

public class ReviewThreadRendererTests
{
    private static readonly QuillConfig Config = new()
    {
        ServerUrl = "https://devops.example.com",
        Collection = "COL",
        Project = "PROJ",
        AllowedStates = ["New"],
    };

    [Fact]
    public async Task RenderAsync_ConvertsEachCommentAndKeepsThreadOrder()
    {
        var threads = new[] { Thread(1, "<p>first</p>"), Thread(2, string.Empty) };

        var rows = await ReviewThreadRenderer.RenderAsync(threads, new RichText(Config, new FakeAzureDevOpsClient(), NullLogger<RichText>.Instance), TestContext.Current.CancellationToken);

        rows.Select(r => r.Id).ShouldBe([1, 2]);
        rows[0].Comments.Single().Text.ShouldBe("first");
        rows[1].Comments.Single().Text.ShouldBe(string.Empty);
    }

    private static PullRequestThread Thread(int id, string textHtml) => new()
    {
        Id = id,
        Status = "active",
        PublishedDate = new DateTimeOffset(2026, 1, 1, 9, 0, 0, TimeSpan.Zero),
        LastUpdatedDate = new DateTimeOffset(2026, 1, 1, 9, 0, 0, TimeSpan.Zero),
        Comments =
        [
            new PullRequestComment
            {
                Id = 1,
                Author = "Jane Doe",
                CreatedDate = new DateTimeOffset(2026, 1, 1, 9, 0, 0, TimeSpan.Zero),
                LastUpdatedDate = new DateTimeOffset(2026, 1, 1, 9, 0, 0, TimeSpan.Zero),
                UsersLiked = [],
                TextHtml = textHtml,
            },
        ],
    };
}
