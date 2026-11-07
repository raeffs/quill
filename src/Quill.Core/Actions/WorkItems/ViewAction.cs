using Quill.Core.Markdown;
using Quill.Core.Models;

namespace Quill.Core.Actions.WorkItems;

public static class ViewAction
{
    public static async Task<ViewResult> RunAsync(
        IAzureDevOpsClient client,
        IWorkItemBatchFetcher fetcher,
        RichText richText,
        int id,
        bool withChildren,
        CancellationToken cancellationToken)
    {
        var workItem = await client.GetWorkItemAsync(id);

        var body = await richText.ToMarkdownAsync(workItem.Description, cancellationToken);

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
