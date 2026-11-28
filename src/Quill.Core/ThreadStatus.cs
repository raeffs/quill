namespace Quill.Core;

public static class ThreadStatus
{
    public static IReadOnlyList<string> All { get; } = ["active", "fixed", "wontFix", "closed", "byDesign", "pending"];

    public static bool IsKnown(string value) => All.Contains(value, StringComparer.Ordinal);
}
