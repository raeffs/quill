using Quill.Core.Models;

namespace Quill.Core;

public interface IAzureDevOpsClient
{
    Task<WorkItem> GetWorkItemAsync(int id);

    Task UpdateWorkItemFieldsAsync(int id, string type, string title, string descriptionHtml);

    Task AddRelationAsync(int sourceId, int targetId);

    Task<int> CreateWorkItemAsync(string type, string title, int parentId, string? assignedToId = null, string? descriptionHtml = null, string? iterationPath = null);

    Task<IReadOnlyList<WorkItemComment>> GetCommentsAsync(int id, int? limit = null, CancellationToken cancellationToken = default);
}
