namespace Quill.Core.Actions.WorkItems;

public sealed record WiListRequest(
    string? Title,
    string? Assignee,
    IReadOnlyList<string> States,
    IReadOnlyList<string> Types,
    int Limit);
