using Quill.Core.Models;

namespace Quill.Core.Tests;

internal sealed class FakeAzureDevOpsClient : IAzureDevOpsClient
{
    private readonly Dictionary<int, WorkItem> _workItems = [];
    private int _nextId = 1000;

    public CreateWorkItemCall? CreateCall { get; private set; }

    public void Add(WorkItem workItem) => _workItems[workItem.Id] = workItem;

    public Task<WorkItem> GetWorkItemAsync(int id) =>
        _workItems.TryGetValue(id, out var workItem)
            ? Task.FromResult(workItem)
            : throw new HttpRequestException($"Work item {id} not found.");

    public Task<int> CreateWorkItemAsync(string type, string title, int parentId, string? assignedToId = null, string? descriptionHtml = null, string? iterationPath = null)
    {
        CreateCall = new CreateWorkItemCall(type, title, parentId, assignedToId, descriptionHtml, iterationPath);

        var id = _nextId++;
        _workItems[id] = new WorkItem
        {
            Id = id,
            Type = type,
            Title = title,
            State = "New",
            AssignedToId = string.Empty,
            AssignedToDisplayName = assignedToId ?? string.Empty,
            Description = descriptionHtml ?? string.Empty,
            IterationPath = iterationPath ?? string.Empty,
            ParentId = parentId,
        };

        return Task.FromResult(id);
    }

    public Task AddRelationAsync(int sourceId, int targetId)
    {
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
            Relations = [.. source.Relations, new WorkItemRelation { RelationType = "System.LinkTypes.Related", TargetId = targetId }],
        };

        return Task.CompletedTask;
    }

    public Task UpdateWorkItemFieldsAsync(int id, string type, string title, string descriptionHtml) =>
        throw new NotSupportedException();

    public Task<IReadOnlyList<WorkItemComment>> GetCommentsAsync(int id, int? limit = null, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
}
