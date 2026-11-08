namespace Quill.Core.Models;

public sealed record Person(string Id, string DisplayName, string? UniqueName)
{
    public string AssignedTo => UniqueName is null ? DisplayName : $"{DisplayName} <{UniqueName}>";
}
