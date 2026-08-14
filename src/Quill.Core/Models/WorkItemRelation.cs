namespace Quill.Core.Models;

public class WorkItemRelation
{
    public required string RelationType { get; init; }

    public required int TargetId { get; init; }
}
