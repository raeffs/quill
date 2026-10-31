using Quill.Core.Validation;

namespace Quill.Core.Models;

public sealed record WorkItemCreateRequest(
    string Type, string Title, int ParentId, string? DescriptionMarkdown, string? Assignee, IReadOnlyList<string>? Tags = null);

public sealed record WorkItemCreateOutcome(ValidationResult Validation, ViewResult? Created);
