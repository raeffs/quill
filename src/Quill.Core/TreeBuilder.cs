using Quill.Core.Models;

namespace Quill.Core;

public static class TreeBuilder
{
    public static async Task<TreeNode> BuildAsync(
        WorkItem root,
        IWorkItemBatchFetcher fetcher,
        int? maxDepth,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(fetcher);
        if (maxDepth is not null && maxDepth < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maxDepth), maxDepth, "maxDepth must be null or ≥ 1.");
        }

        var rootBuilder = BuildNode.Fetched(root);
        var currentLevel = new List<(BuildNode Node, WorkItem Item)> { (rootBuilder, root) };

        for (var depth = 1; maxDepth is null || depth <= maxDepth; depth++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var allChildIds = currentLevel.SelectMany(p => p.Item.ChildIds).ToArray();
            if (allChildIds.Length == 0)
            {
                return rootBuilder.Freeze();
            }

            var fetched = await fetcher.FetchAsync(allChildIds, cancellationToken);
            var itemsById = fetched.Items.ToDictionary(i => i.Id);
            var batchFailed = new HashSet<int>(fetched.BatchFailedIds);

            var nextLevel = new List<(BuildNode Node, WorkItem Item)>();
            foreach (var (parentNode, parentItem) in currentLevel)
            {
                foreach (var id in parentItem.ChildIds)
                {
                    if (itemsById.TryGetValue(id, out var item))
                    {
                        var childNode = BuildNode.Fetched(item);
                        parentNode.Children!.Add(childNode);
                        nextLevel.Add((childNode, item));
                    }
                    else if (batchFailed.Contains(id))
                    {
                        parentNode.Children!.Add(BuildNode.Stub(id, "batch-failed"));
                    }
                    else
                    {
                        parentNode.Children!.Add(BuildNode.Stub(id, "unreadable"));
                    }
                }
            }

            currentLevel = nextLevel;
        }

        // Depth limit reached; emit id-only stubs for any remaining unfetched children.
        foreach (var (parentNode, parentItem) in currentLevel)
        {
            foreach (var id in parentItem.ChildIds)
            {
                parentNode.Children!.Add(BuildNode.IdOnlyStub(id));
            }
        }

        return rootBuilder.Freeze();
    }

    private sealed class BuildNode
    {
        private BuildNode(int id, string? title, string? type, string? state, string? error, List<BuildNode>? children)
        {
            Id = id;
            Title = title;
            Type = type;
            State = state;
            Error = error;
            Children = children;
        }

        public int Id { get; }

        public string? Title { get; }

        public string? Type { get; }

        public string? State { get; }

        public string? Error { get; }

        public List<BuildNode>? Children { get; }

        public static BuildNode Fetched(WorkItem item)
        {
            return new BuildNode(item.Id, item.Title, item.Type, item.State, error: null, children: new List<BuildNode>());
        }

        public static BuildNode Stub(int id, string error)
        {
            return new BuildNode(id, title: null, type: null, state: null, error, children: null);
        }

        public static BuildNode IdOnlyStub(int id)
        {
            return new BuildNode(id, title: null, type: null, state: null, error: null, children: null);
        }

        public TreeNode Freeze()
        {
            return new TreeNode
            {
                Id = Id,
                Title = Title,
                Type = Type,
                State = State,
                Error = Error,
                Children = Children?.Select(c => c.Freeze()).ToArray(),
            };
        }
    }
}
