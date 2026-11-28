namespace Quill.Core.Models;

public sealed record Identity(string Id, string DisplayName, string? UniqueName, bool IsGroup)
{
    public string AssignedTo
    {
        get
        {
            if (IsGroup)
            {
                throw new InvalidOperationException($"The group '{DisplayName}' cannot be assigned a work item.");
            }

            return UniqueName is null ? DisplayName : $"{DisplayName} <{UniqueName}>";
        }
    }
}
