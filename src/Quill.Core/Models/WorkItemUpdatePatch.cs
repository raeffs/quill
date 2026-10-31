namespace Quill.Core.Models;

public sealed record WorkItemUpdatePatch(int Id, string Type, string Title, string DescriptionHtml)
{
    public IReadOnlyList<string>? Tags { get; init; }

    public IReadOnlyList<WorkItemRelation> AddRelations { get; init; } = [];
}
