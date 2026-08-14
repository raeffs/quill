namespace Quill.Core.Models;

public class QuillConfig
{
    public string ServerUrl { get; init; } = string.Empty;

    public string Collection { get; init; } = string.Empty;

    public string Project { get; init; } = string.Empty;

    public IReadOnlyList<string> AllowedStates { get; init; } = [];

    public IReadOnlyList<string> AllowedParentStates { get; init; } = [];
}
