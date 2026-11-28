namespace Quill.Core;

public sealed record PersonEntry(string Id, string DisplayName, string? UniqueName, DateTimeOffset SavedAt);
