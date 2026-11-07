using System.Globalization;
using Quill.Core.Models;

namespace Quill.Core.Tests;

internal sealed class FakeAzureDevOpsClient : IAzureDevOpsClient, IWorkItemBatchFetcher
{
    private readonly Dictionary<int, WorkItem> _workItems = [];
    private int _nextId = 1000;

    public WorkItemCreatePatch? CreateCall { get; private set; }

    public HashSet<int> BatchFailedIds { get; } = [];

#pragma warning disable CA1002, MA0016 // Tests assert on the recorded calls as a list
    public List<string> Calls { get; } = [];
#pragma warning restore CA1002, MA0016

    public Action<WorkItemUpdatePatch> UpdateWorkItem { get; set; } = _ => { };

    public Func<string, int, IReadOnlyList<int>> QueryByWiql { get; set; } = (_, _) => throw new NotSupportedException();

    public Func<int, int?, IReadOnlyList<WorkItemComment>> GetComments { get; set; } = (_, _) => throw new NotSupportedException();

    public void Add(WorkItem workItem) => _workItems[workItem.Id] = workItem;

    public Task<WorkItem> GetWorkItemAsync(int id)
    {
        Calls.Add($"GetWorkItem({id})");
        return _workItems.TryGetValue(id, out var workItem)
            ? Task.FromResult(workItem)
            : throw new HttpRequestException($"Work item {id} not found.");
    }

    public Task<int> CreateWorkItemAsync(WorkItemCreatePatch patch)
    {
        Calls.Add("CreateWorkItem");
        CreateCall = patch;

        var id = _nextId++;
        _workItems[id] = new WorkItem
        {
            Id = id,
            Type = patch.Type,
            Title = patch.Title,
            State = "New",
            AssignedToId = string.Empty,
            AssignedToDisplayName = patch.AssignedToId ?? string.Empty,
            Description = patch.DescriptionHtml ?? string.Empty,
            IterationPath = patch.IterationPath ?? string.Empty,
            ParentId = patch.ParentId,
            Tags = patch.Tags ?? [],
            Predecessors = (patch.Predecessors ?? []).Select((id, index) => new WorkItemPredecessor(id, index)).ToList(),
        };

        return Task.FromResult(id);
    }

    public Task AddRelationAsync(int sourceId, int targetId)
    {
        Calls.Add($"AddRelation({sourceId},{targetId})");
        var source = _workItems[sourceId];
        _workItems[sourceId] = new WorkItem
        {
            Id = source.Id,
            Type = source.Type,
            Title = source.Title,
            State = source.State,
            AssignedToId = source.AssignedToId,
            AssignedToDisplayName = source.AssignedToDisplayName,
            Description = source.Description,
            IterationPath = source.IterationPath,
            ParentId = source.ParentId,
            Tags = source.Tags,
            Predecessors = source.Predecessors,
            Relations = [.. source.Relations, new WorkItemRelation { RelationType = "System.LinkTypes.Related", TargetId = targetId }],
        };

        return Task.CompletedTask;
    }

    public Task UpdateWorkItemAsync(WorkItemUpdatePatch patch)
    {
        Calls.Add($"UpdateWorkItem({patch.Id})");
        UpdateWorkItem(patch);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<WorkItemComment>> GetCommentsAsync(int id, int? limit = null, CancellationToken cancellationToken = default)
    {
        Calls.Add($"GetComments({id})");
        return Task.FromResult(GetComments(id, limit));
    }

    public Task<IReadOnlyList<int>> QueryByWiqlAsync(string wiql, int top, CancellationToken cancellationToken = default)
    {
        Calls.Add("QueryByWiql");
        return Task.FromResult(QueryByWiql(wiql, top));
    }

    public Task<BatchFetchResult> FetchAsync(IReadOnlyList<int> ids, CancellationToken cancellationToken = default)
    {
        Calls.Add(string.Create(CultureInfo.InvariantCulture, $"Fetch({string.Join(',', ids)})"));
        return Task.FromResult(new BatchFetchResult
        {
            Items = [.. ids.Where(_workItems.ContainsKey).Select(id => _workItems[id])],
            BatchFailedIds = [.. ids.Where(BatchFailedIds.Contains)],
        });
    }
}
