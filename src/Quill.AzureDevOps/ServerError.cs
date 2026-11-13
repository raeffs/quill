namespace Quill.AzureDevOps;

public sealed record ServerError(int Status, string? Message, string? TypeKey);
