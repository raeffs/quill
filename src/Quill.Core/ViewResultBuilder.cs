using Quill.Core.Models;

namespace Quill.Core;

public static class ViewResultBuilder
{
    public static ViewResult Build(WorkItem workItem, string markdownBody, IReadOnlyList<ChildItem>? children)
    {
        return new ViewResult
        {
            Id = workItem.Id,
            Type = workItem.Type,
            Title = workItem.Title,
            State = workItem.State,
            AssignedTo = string.IsNullOrEmpty(workItem.AssignedToDisplayName) ? null : workItem.AssignedToDisplayName,
            ParentId = workItem.ParentId,
            Description = markdownBody,
            RelatedIds = workItem.Relations.Select(r => r.TargetId).ToList(),
            Children = children,
        };
    }
}
