namespace Quill.Core.Models;

public sealed record WorkItemCreatePatch(
    string Type,
    string Title,
    int ParentId,
    Person? Assignee = null,
    string? DescriptionHtml = null,
    string? IterationPath = null,
    string? AreaPath = null,
    IReadOnlyList<string>? Tags = null,
    IReadOnlyList<int>? Predecessors = null);
