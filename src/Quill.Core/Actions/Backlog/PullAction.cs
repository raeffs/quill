using Microsoft.Extensions.Logging;
using Quill.Core.Markdown;
using Quill.Core.Models;

namespace Quill.Core.Actions.Backlog;

public sealed record BacklogPullOutcome(int Id, string Title, string FileContent);

public static class PullAction
{
    public static async Task<BacklogPullOutcome> RunAsync(
        IAzureDevOpsClient client,
        QuillConfig config,
        ILogger logger,
        int id)
    {
        var workItem = await client.GetWorkItemAsync(id);

        var markdownBody = string.IsNullOrEmpty(workItem.Description)
            ? string.Empty
            : await MarkdownConverter.ToMarkdownAsync(
                workItem.Description, config.ServerUrl, config.Collection, config.Project, client, logger);

        var fileContent = FrontmatterParser.Write(
            id: workItem.Id,
            type: workItem.Type,
            title: workItem.Title,
            state: workItem.State,
            body: markdownBody.TrimEnd(),
            parentId: workItem.ParentId,
            tags: workItem.Tags,
            predecessors: workItem.Predecessors.Select(p => p.Id).ToList());

        return new BacklogPullOutcome(workItem.Id, workItem.Title, fileContent);
    }
}
