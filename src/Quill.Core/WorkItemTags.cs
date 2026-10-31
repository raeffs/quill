namespace Quill.Core;

/// <summary>
/// The rules for a tag and the single string Azure DevOps keeps them in.
/// Length, count and tag-creation permission stay with the server.
/// </summary>
public static class WorkItemTags
{
    public static IReadOnlyList<string> Normalize(IEnumerable<string?> tags)
    {
        var normalized = new List<string>();

        foreach (var tag in tags)
        {
            var trimmed = tag?.Trim();

            if (string.IsNullOrEmpty(trimmed))
            {
                throw new InvalidOperationException("A tag must not be empty.");
            }

            if (trimmed.Contains(';', StringComparison.Ordinal) || trimmed.Contains(',', StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Tag '{trimmed}' must not contain ';' or ','.");
            }

            if (!normalized.Contains(trimmed, StringComparer.Ordinal))
            {
                normalized.Add(trimmed);
            }
        }

        return normalized;
    }

    public static string Join(IEnumerable<string> tags) => string.Join("; ", tags);

    public static IReadOnlyList<string> Split(string? value) =>
        string.IsNullOrEmpty(value)
            ? []
            : value.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
}
