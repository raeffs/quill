namespace Quill.Core;

public sealed record PredecessorChange(IReadOnlyList<int> Add, IReadOnlyList<int> RemoveRelationIndexes)
{
    public bool HasChanges => Add.Count > 0 || RemoveRelationIndexes.Count > 0;
}
