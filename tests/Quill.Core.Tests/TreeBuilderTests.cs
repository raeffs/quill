using System.Text.Json;
using System.Text.Json.Serialization;
using Quill.Core;
using Quill.Core.Models;
using Shouldly;

namespace Quill.Core.Tests;

public class TreeBuilderTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        TypeInfoResolver = TestJsonContext.Default,
    };

    [Fact]
    public async Task BuildAsync_RootWithNoChildren_ReturnsFetchedRootWithEmptyChildren()
    {
        var root = MakeItem(1, "Epic", "Root", "New");
        var fetcher = new FakeBatchFetcher();

        var tree = await TreeBuilder.BuildAsync(root, fetcher, maxDepth: 3, cancellationToken: TestContext.Current.CancellationToken);

        tree.Id.ShouldBe(1);
        tree.Title.ShouldBe("Root");
        tree.Type.ShouldBe("Epic");
        tree.State.ShouldBe("New");
        tree.Error.ShouldBeNull();
        tree.Children.ShouldNotBeNull();
        tree.Children.ShouldBeEmpty();
        fetcher.Calls.ShouldBeEmpty();
    }

    [Fact]
    public async Task BuildAsync_Depth1_FetchesDirectChildrenOnlyAndClipsGrandchildrenAsIdStubs()
    {
        var root = MakeItem(1, "Epic", "Root", "New", 10, 11);
        var child10 = MakeItem(10, "Feature", "F-ten", "Active", 100, 101);
        var child11 = MakeItem(11, "Feature", "F-eleven", "New");
        var fetcher = new FakeBatchFetcher(items: [child10, child11]);

        var tree = await TreeBuilder.BuildAsync(root, fetcher, maxDepth: 1, cancellationToken: TestContext.Current.CancellationToken);

        tree.Children!.Count.ShouldBe(2);
        var c10 = tree.Children[0];
        c10.Id.ShouldBe(10);
        c10.Title.ShouldBe("F-ten");
        c10.Type.ShouldBe("Feature");
        c10.State.ShouldBe("Active");
        c10.Children!.Count.ShouldBe(2);
        c10.Children[0].Id.ShouldBe(100);
        c10.Children[0].Title.ShouldBeNull();
        c10.Children[0].Error.ShouldBeNull();
        c10.Children[0].Children.ShouldBeNull();
        c10.Children[1].Id.ShouldBe(101);
        c10.Children[1].Title.ShouldBeNull();

        var c11 = tree.Children[1];
        c11.Id.ShouldBe(11);
        c11.Children.ShouldNotBeNull();
        c11.Children.ShouldBeEmpty();

        fetcher.Calls.Count.ShouldBe(1);
        fetcher.Calls[0].ShouldBe([10, 11]);
    }

    [Fact]
    public async Task BuildAsync_OneBatchCallPerLevel_CollectsAcrossSiblingsIntoSingleCall()
    {
        var root = MakeItem(1, "Epic", "Root", "New", 10, 11);
        var child10 = MakeItem(10, "Feature", "F10", "New", 100, 101);
        var child11 = MakeItem(11, "Feature", "F11", "New", 110);
        var gc100 = MakeItem(100, "PBI", "P100", "New");
        var gc101 = MakeItem(101, "PBI", "P101", "New");
        var gc110 = MakeItem(110, "PBI", "P110", "New");
        var fetcher = new FakeBatchFetcher(items: [child10, child11, gc100, gc101, gc110]);

        _ = await TreeBuilder.BuildAsync(root, fetcher, maxDepth: 2, cancellationToken: TestContext.Current.CancellationToken);

        fetcher.Calls.Count.ShouldBe(2);
        fetcher.Calls[0].ShouldBe([10, 11]);
        fetcher.Calls[1].ShouldBe([100, 101, 110]);
    }

    [Fact]
    public async Task BuildAsync_UnreadableChild_EmitsUnreadableErrorStubInOrder()
    {
        var root = MakeItem(1, "Epic", "Root", "New", 10, 11, 12);
        var child10 = MakeItem(10, "Feature", "F10", "New");
        var child12 = MakeItem(12, "Feature", "F12", "New");
        var fetcher = new FakeBatchFetcher(items: [child10, child12], unreadable: [11]);

        var tree = await TreeBuilder.BuildAsync(root, fetcher, maxDepth: 1, cancellationToken: TestContext.Current.CancellationToken);

        tree.Children!.Count.ShouldBe(3);
        tree.Children[0].Id.ShouldBe(10);
        tree.Children[0].Title.ShouldBe("F10");
        tree.Children[1].Id.ShouldBe(11);
        tree.Children[1].Error.ShouldBe("unreadable");
        tree.Children[1].Title.ShouldBeNull();
        tree.Children[1].Children.ShouldBeNull();
        tree.Children[2].Id.ShouldBe(12);
        tree.Children[2].Title.ShouldBe("F12");
    }

    [Fact]
    public async Task BuildAsync_BatchFailedChild_EmitsBatchFailedErrorStub()
    {
        var root = MakeItem(1, "Epic", "Root", "New", 10, 11);
        var child11 = MakeItem(11, "Feature", "F11", "New");
        var fetcher = new FakeBatchFetcher(items: [child11], batchFailed: [10]);

        var tree = await TreeBuilder.BuildAsync(root, fetcher, maxDepth: 1, cancellationToken: TestContext.Current.CancellationToken);

        tree.Children!.Count.ShouldBe(2);
        tree.Children[0].Id.ShouldBe(10);
        tree.Children[0].Error.ShouldBe("batch-failed");
        tree.Children[1].Id.ShouldBe(11);
        tree.Children[1].Title.ShouldBe("F11");
    }

    [Fact]
    public async Task BuildAsync_UnboundedDepth_FetchesUntilNoMoreChildren()
    {
        var root = MakeItem(1, "Epic", "Root", "New", 10);
        var child10 = MakeItem(10, "Feature", "F10", "New", 100);
        var gc100 = MakeItem(100, "PBI", "P100", "New", 1000);
        var ggc1000 = MakeItem(1000, "Task", "T1000", "New");
        var fetcher = new FakeBatchFetcher(items: [child10, gc100, ggc1000]);

        var tree = await TreeBuilder.BuildAsync(root, fetcher, maxDepth: null, cancellationToken: TestContext.Current.CancellationToken);

        var leaf = tree.Children!.Single().Children!.Single().Children!.Single();
        leaf.Id.ShouldBe(1000);
        leaf.Title.ShouldBe("T1000");
        leaf.Children.ShouldNotBeNull();
        leaf.Children.ShouldBeEmpty();
        fetcher.Calls.Count.ShouldBe(3);
    }

    [Fact]
    public async Task BuildAsync_MaxDepthZero_Throws()
    {
        var root = MakeItem(1, "Epic", "Root", "New");
        var fetcher = new FakeBatchFetcher();

        await Should.ThrowAsync<ArgumentOutOfRangeException>(
            () => TreeBuilder.BuildAsync(root, fetcher, maxDepth: 0));
    }

    [Fact]
    public async Task BuildAsync_JsonShape_FetchedNodeOmitsErrorAndIncludesChildrenArray()
    {
        var root = MakeItem(1, "Epic", "R", "New", 2);
        var fetcher = new FakeBatchFetcher(items: [MakeItem(2, "Feature", "F", "Active")]);
        var tree = await TreeBuilder.BuildAsync(root, fetcher, maxDepth: 1, cancellationToken: TestContext.Current.CancellationToken);

        var json = JsonSerializer.Serialize(tree, JsonOptions);
        json.ShouldBe("""{"id":1,"title":"R","type":"Epic","state":"New","children":[{"id":2,"title":"F","type":"Feature","state":"Active","children":[]}]}""");
    }

    [Fact]
    public async Task BuildAsync_JsonShape_IdOnlyStubAndUnreadableStubSerializeMinimally()
    {
        var root = MakeItem(1, "Epic", "R", "New", 10, 11);
        var fetcher = new FakeBatchFetcher(unreadable: [10]);
        var tree = await TreeBuilder.BuildAsync(root, fetcher, maxDepth: 1, cancellationToken: TestContext.Current.CancellationToken);

        var json = JsonSerializer.Serialize(tree, JsonOptions);
        json.ShouldContain("""{"id":10,"error":"unreadable"}""");
        json.ShouldContain("""{"id":11,"error":"unreadable"}""");
        json.ShouldNotContain("null");
    }

    private static WorkItem MakeItem(int id, string type, string title, string state, params int[] childIds)
    {
        return new WorkItem
        {
            Id = id,
            Type = type,
            Title = title,
            State = state,
            AssignedToId = string.Empty,
            ChildIds = childIds,
        };
    }

    private sealed class FakeBatchFetcher : IWorkItemBatchFetcher
    {
        private readonly Dictionary<int, WorkItem> _items;
        private readonly HashSet<int> _unreadable;
        private readonly HashSet<int> _batchFailed;

        public FakeBatchFetcher(
            IEnumerable<WorkItem>? items = null,
            IEnumerable<int>? unreadable = null,
            IEnumerable<int>? batchFailed = null)
        {
            _items = (items ?? Array.Empty<WorkItem>()).ToDictionary(i => i.Id);
            _unreadable = new HashSet<int>(unreadable ?? Array.Empty<int>());
            _batchFailed = new HashSet<int>(batchFailed ?? Array.Empty<int>());
        }

        public List<IReadOnlyList<int>> Calls { get; } = new();

        public Task<BatchFetchResult> FetchAsync(IReadOnlyList<int> ids, CancellationToken cancellationToken = default)
        {
            Calls.Add(ids.ToArray());
            var items = ids
                .Where(id => _items.ContainsKey(id) && !_unreadable.Contains(id) && !_batchFailed.Contains(id))
                .Select(id => _items[id])
                .ToArray();
            var batchFailedIds = ids.Where(id => _batchFailed.Contains(id)).ToArray();
            return Task.FromResult(new BatchFetchResult
            {
                Items = items,
                BatchFailedIds = batchFailedIds,
            });
        }
    }
}

[JsonSerializable(typeof(TreeNode))]
internal sealed partial class TestJsonContext : JsonSerializerContext
{
}
