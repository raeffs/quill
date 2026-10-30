namespace Quill.Core.Tests;

internal sealed record CreateWorkItemCall(
    string Type, string Title, int ParentId, string? AssignedToId, string? DescriptionHtml, string? IterationPath);
