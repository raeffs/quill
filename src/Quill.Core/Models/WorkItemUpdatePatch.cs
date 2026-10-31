namespace Quill.Core.Models;

public sealed record WorkItemUpdatePatch(int Id, string Type, string Title, string DescriptionHtml)
{
    public IReadOnlyList<WorkItemRelation> AddRelations { get; init; } = [];
}
