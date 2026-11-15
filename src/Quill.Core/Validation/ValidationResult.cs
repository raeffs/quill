namespace Quill.Core.Validation;

public record ValidationResult(bool IsValid, IReadOnlyList<string> Errors);
