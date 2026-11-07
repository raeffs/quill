using Quill.Core.Markdown;
using Quill.Core.Models;

namespace Quill.Core.Actions.Backlog;

public sealed record BacklogPullOutcome(int Id, string Title, string FileContent);

public static class PullAction
{
    public static async Task<BacklogPullOutcome> RunAsync(
        IAzureDevOpsClient client,
        RichText richText,
        int id)
    {
        var workItem = await client.GetWorkItemAsync(id);

        var markdownBody = await richText.ToMarkdownAsync(workItem.Description);

        var fileContent = FrontmatterParser.Write(
            id: workItem.Id,
            type: workItem.Type,
            title: workItem.Title,
            state: workItem.State,
            body: markdownBody,
            parentId: workItem.ParentId,
            tags: workItem.Tags,
            predecessors: workItem.Predecessors.Select(p => p.Id).ToList());

        return new BacklogPullOutcome(workItem.Id, workItem.Title, fileContent);
    }
}
