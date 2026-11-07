using Microsoft.Extensions.Logging;
using Quill.Core.Markdown;
using Quill.Core.Models;

namespace Quill.Core.Actions;

public static class WiViewAction
{
    public static async Task<ViewResult> RunAsync(
        IAzureDevOpsClient client,
        IWorkItemBatchFetcher fetcher,
        QuillConfig config,
        ILogger logger,
        int id,
        bool withChildren,
        CancellationToken cancellationToken)
    {
        var workItem = await client.GetWorkItemAsync(id);

        var body = string.IsNullOrEmpty(workItem.Description)
            ? string.Empty
            : (await MarkdownConverter.ToMarkdownAsync(
                workItem.Description, config.ServerUrl, config.Collection, config.Project, client, logger)).TrimEnd();

        IReadOnlyList<ChildItem>? children = null;
        if (withChildren)
        {
            var batch = await fetcher.FetchAsync(workItem.ChildIds, cancellationToken);
            children = batch.Items
                .Select(i => new ChildItem { Id = i.Id, Title = i.Title, State = i.State })
                .ToList();
        }

        return ViewResultBuilder.Build(workItem, body, children);
    }
}
