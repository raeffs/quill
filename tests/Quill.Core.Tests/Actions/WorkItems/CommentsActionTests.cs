using Microsoft.Extensions.Logging.Abstractions;
using Quill.Core.Actions.WorkItems;
using Quill.Core.Markdown;
using Quill.Core.Models;
using Shouldly;

namespace Quill.Core.Tests.Actions.WorkItems;

public class CommentsActionTests
{
    private static readonly QuillConfig Config = new()
    {
        ServerUrl = "https://devops.example.com",
        Collection = "COL",
        Project = "PROJ",
        AllowedStates = ["New"],
    };

    [Fact]
    public async Task RunAsync_PassesTheLimitOnAndConvertsTheCommentToMarkdown()
    {
        int? seenLimit = null;
        var client = new FakeAzureDevOpsClient
        {
            GetComments = (_, limit) =>
            {
                seenLimit = limit;
                return
                [
                    new WorkItemComment
                    {
                        Id = 1,
                        Author = "Jane Doe",
                        CreatedDate = new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero),
                        TextHtml = "<p>hi</p>",
                    },
                ];
            },
        };

        var rows = await CommentsAction.RunAsync(client, new RichText(Config, client, NullLogger<RichText>.Instance), 5, 2, TestContext.Current.CancellationToken);

        seenLimit.ShouldBe(2);
        client.Calls.ShouldBe(["GetComments(5)"]);
        rows.Count.ShouldBe(1);
        rows[0].Text.ShouldBe("hi");
    }
}
