using System.Globalization;
using Quill.Core.Models;

namespace Quill.Core;

public sealed record PredecessorChange(IReadOnlyList<int> Add, IReadOnlyList<int> RemoveRelationIndexes)
{
    public bool HasChanges => Add.Count > 0 || RemoveRelationIndexes.Count > 0;
}

/// <summary>
/// The rules for a predecessor ID and the link operations that make a work item
/// match a file. Missing targets, cycles and self-links stay with the server.
/// </summary>
public static class WorkItemPredecessors
{
    public static IReadOnlyList<int> Validate(IEnumerable<int> ids)
    {
        var validated = new List<int>();

        foreach (var id in ids)
        {
            var text = id.ToString(CultureInfo.InvariantCulture);

            if (id <= 0)
            {
                throw new InvalidOperationException($"Predecessor '{text}' must be a positive work item ID.");
            }

            if (validated.Contains(id))
            {
                throw new InvalidOperationException($"Predecessor '{text}' is listed more than once.");
            }

            validated.Add(id);
        }

        return validated;
    }

    // Descending, because each removal shifts the indexes after it.
    public static PredecessorChange Diff(IReadOnlyList<WorkItemPredecessor> current, IReadOnlyList<int>? desired)
    {
        if (desired is null)
        {
            return new PredecessorChange([], []);
        }

        var add = desired.Where(id => current.All(p => p.Id != id)).ToList();
        var remove = current
            .Where(p => !desired.Contains(p.Id))
            .Select(p => p.RelationIndex)
            .OrderDescending()
            .ToList();

        return new PredecessorChange(add, remove);
    }
}
