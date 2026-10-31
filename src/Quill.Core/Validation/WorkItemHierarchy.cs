namespace Quill.Core.Validation;

public static class WorkItemHierarchy
{
    private static readonly Dictionary<string, string[]> ParentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Feature"] = ["Epic"],
        ["Product Backlog Item"] = ["Feature"],
        ["Bug"] = ["Feature"],
        ["Task"] = ["Product Backlog Item", "Bug"],
    };

    public static IReadOnlyList<string> ChildTypes { get; } = ["Feature", "Product Backlog Item", "Bug", "Task"];

    public static IReadOnlyList<string> ParentTypesOf(string childType) =>
        ParentTypes.TryGetValue(childType, out var parentTypes) ? parentTypes : [];

    public static string? CanonicalChildType(string type) =>
        ChildTypes.FirstOrDefault(childType => string.Equals(childType, type, StringComparison.OrdinalIgnoreCase));
}
