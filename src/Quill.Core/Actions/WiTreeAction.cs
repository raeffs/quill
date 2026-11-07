using Quill.Core.Models;

namespace Quill.Core.Actions;

public static class WiTreeAction
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
