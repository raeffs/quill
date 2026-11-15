namespace Quill.Core.Markdown;

public record ParsedMarkdownFile(int Id, string? Type, string Title, string? State, string Body, int? ParentId = null, IReadOnlyList<string>? Tags = null, IReadOnlyList<int>? Predecessors = null);
