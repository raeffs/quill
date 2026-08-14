using Quill.Core.Models;

namespace Quill.Core;

public static class SearchResultBuilder
{
    public static SearchResult Build(WorkItem workItem)
    {
        return new SearchResult
        {
            Id = workItem.Id,
            Title = workItem.Title,
            State = workItem.State,
            Type = workItem.Type,
            AssignedTo = string.IsNullOrEmpty(workItem.AssignedToDisplayName) ? null : workItem.AssignedToDisplayName,
            ParentId = workItem.ParentId,
        };
    }
}
