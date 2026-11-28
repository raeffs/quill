namespace Quill.Core;

public sealed record PersonEntry(string Id, string DisplayName, string? UniqueName, bool IsGroup, DateTimeOffset SavedAt);
