using Quill.Core.Models;

namespace Quill.Core;

public interface IAzureDevOpsClient
{
    Task<WorkItem> GetWorkItemAsync(int id);

    Task UpdateWorkItemAsync(WorkItemUpdatePatch patch);

    Task AddRelationAsync(int sourceId, int targetId);

    Task<int> CreateWorkItemAsync(WorkItemCreatePatch patch);

    Task<IReadOnlyList<WorkItemComment>> GetCommentsAsync(int id, int? limit = null, CancellationToken cancellationToken = default);
}
