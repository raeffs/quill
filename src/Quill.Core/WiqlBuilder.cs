using Quill.Core.Models;

namespace Quill.Core;

public static class WiqlBuilder
{
    public static (string Wiql, int Top) Build(
        string? title,
        PersonFilter? assignee,
        IReadOnlyList<string> states,
        IReadOnlyList<string> types,
        int limit)
    {
        var hasTitle = !string.IsNullOrWhiteSpace(title);
        var hasAssignee = assignee is not null;
        var hasStates = states is { Count: > 0 };
        var hasTypes = types is { Count: > 0 };

        if (!hasTitle && !hasAssignee && !hasStates && !hasTypes)
        {
            throw new UsageException("provide at least one filter");
        }

        if (limit <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(limit), "limit must be positive");
        }

        var clauses = new List<string>();

        if (hasTitle)
        {
            clauses.Add($"[System.Title] CONTAINS WORDS '{Escape(title!)}'");
        }

        if (hasAssignee)
        {
            clauses.Add(assignee!.Person is null
                ? "[System.AssignedTo] = @Me"
                : $"[System.AssignedTo] = '{Escape(assignee.Person.AssignedTo)}'");
        }

        if (hasStates)
        {
            clauses.Add($"[System.State] IN ({JoinLiterals(states)})");
        }

        if (hasTypes)
        {
            clauses.Add($"[System.WorkItemType] IN ({JoinLiterals(types)})");
        }

        var wiql =
            "SELECT [System.Id] FROM WorkItems WHERE " +
            string.Join(" AND ", clauses) +
            " ORDER BY [System.ChangedDate] DESC";

        return (wiql, limit);
    }

    private static string JoinLiterals(IReadOnlyList<string> values)
    {
        return string.Join(", ", values.Select(v => $"'{Escape(v)}'"));
    }

    private static string Escape(string value) => value.Replace("'", "''", StringComparison.Ordinal);
}
