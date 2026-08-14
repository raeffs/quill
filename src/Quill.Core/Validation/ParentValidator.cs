using Quill.Core.Models;

namespace Quill.Core.Validation;

public static class ParentValidator
{
    public static ValidationResult Validate(
        WorkItem parentWorkItem,
        QuillConfig config,
        string currentUserId)
    {
        var errors = new List<string>();

        if (!config.AllowedParentStates.Contains(parentWorkItem.State, StringComparer.OrdinalIgnoreCase))
        {
            errors.Add($"Parent work item state '{parentWorkItem.State}' is not in allowed parent states: [{string.Join(", ", config.AllowedParentStates)}]");
        }

        if (!string.Equals(parentWorkItem.AssignedToId, currentUserId, StringComparison.OrdinalIgnoreCase))
        {
            errors.Add("Parent work item assignee does not match the current user.");
        }

        return new ValidationResult(errors.Count == 0, errors);
    }
}
