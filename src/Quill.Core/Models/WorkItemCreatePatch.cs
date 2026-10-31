namespace Quill.Core.Models;

public sealed record WorkItemCreatePatch(
    string Type,
    string Title,
    int ParentId,
    string? AssignedToId = null,
    string? DescriptionHtml = null,
    string? IterationPath = null);
