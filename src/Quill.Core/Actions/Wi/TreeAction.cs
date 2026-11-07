using Quill.Core.Models;

namespace Quill.Core.Actions.Wi;

public static class TreeAction
{
    public static async Task<TreeNode> RunAsync(
        IAzureDevOpsClient client,
        IWorkItemBatchFetcher fetcher,
        int id,
        int? maxDepth,
        CancellationToken cancellationToken)
    {
        var root = await client.GetWorkItemAsync(id);

        return await TreeBuilder.BuildAsync(root, fetcher, maxDepth, cancellationToken);
    }
}
