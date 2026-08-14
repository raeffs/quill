namespace Quill.Core;

public static class WiqlBuilder
{
    public static (string Wiql, int Top) Build(
        string? query,
        string? assignee,
        IReadOnlyList<string> states,
        IReadOnlyList<string> types,
        int limit)
    {
        var hasQuery = !string.IsNullOrWhiteSpace(query);
        var hasAssignee = !string.IsNullOrWhiteSpace(assignee);
        var hasStates = states is { Count: > 0 };
        var hasTypes = types is { Count: > 0 };

        if (!hasQuery && !hasAssignee && !hasStates && !hasTypes)
        {
            throw new InvalidOperationException("provide a query or at least one filter");
        }

        if (limit <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(limit), "limit must be positive");
        }

        var clauses = new List<string>();

        if (hasQuery)
        {
            clauses.Add($"[System.Title] CONTAINS WORDS '{Escape(query!)}'");
        }

        if (hasAssignee)
        {
            var trimmed = assignee!.Trim();
            clauses.Add(string.Equals(trimmed, "@me", StringComparison.OrdinalIgnoreCase)
                ? "[System.AssignedTo] = @Me"
                : $"[System.AssignedTo] = '{Escape(trimmed)}'");
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
