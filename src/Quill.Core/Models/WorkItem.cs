namespace Quill.Core.Models;

public class WorkItem
{
    public required int Id { get; init; }

    public required string Type { get; init; }

    public required string Title { get; set; }

    public required string State { get; init; }

    public required string AssignedToId { get; init; }

    public string AssignedToDisplayName { get; init; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string IterationPath { get; init; } = string.Empty;

    public int? ParentId { get; init; }

    public IReadOnlyList<WorkItemRelation> Relations { get; init; } = [];

    public IReadOnlyList<int> ChildIds { get; init; } = [];
}
