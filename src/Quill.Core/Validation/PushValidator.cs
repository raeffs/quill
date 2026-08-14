using Quill.Core.Models;

namespace Quill.Core.Validation;

public record ValidationResult(bool IsValid, IReadOnlyList<string> Errors);

public static class PushValidator
{
    public static ValidationResult Validate(
        WorkItem workItem,
        QuillConfig config,
        string currentUserId)
    {
        var errors = new List<string>();

        if (!config.AllowedStates.Contains(workItem.State, StringComparer.OrdinalIgnoreCase))
        {
            errors.Add($"Work item state '{workItem.State}' is not in allowed states: [{string.Join(", ", config.AllowedStates)}]");
        }

        if (!string.Equals(workItem.AssignedToId, currentUserId, StringComparison.OrdinalIgnoreCase))
        {
            errors.Add($"Work item assignee does not match the current user.");
        }

        return new ValidationResult(errors.Count == 0, errors);
    }
}
