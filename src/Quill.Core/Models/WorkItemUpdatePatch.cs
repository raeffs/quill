namespace Quill.Core.Models;

public sealed record WorkItemUpdatePatch(int Id, string Type, string Title, string DescriptionHtml)
{
    public IReadOnlyList<string>? Tags { get; init; }

    public IReadOnlyList<WorkItemRelation> AddRelations { get; init; } = [];

    /// <summary>Sent in this order, so list the highest index first.</summary>
    public IReadOnlyList<int> RemoveRelationIndexes { get; init; } = [];

    /// <summary>The revision the removal indexes were read from.</summary>
    public int Rev { get; init; }
}
