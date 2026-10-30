using Quill.Core.Models;

namespace Quill.Core.Validation;

public static class ParentValidator
{
    public static ValidationResult Validate(
        string childType,
        WorkItem parentWorkItem,
        string currentUserId)
    {
        var errors = new List<string>(ValidateType(childType, parentWorkItem).Errors);

        if (!string.Equals(parentWorkItem.AssignedToId, currentUserId, StringComparison.OrdinalIgnoreCase))
        {
            errors.Add("Parent work item assignee does not match the current user.");
        }

        return new ValidationResult(errors.Count == 0, errors);
    }

    public static ValidationResult ValidateType(string childType, WorkItem parentWorkItem)
    {
        var parentTypes = WorkItemHierarchy.ParentTypesOf(childType);

        if (parentTypes.Contains(parentWorkItem.Type, StringComparer.OrdinalIgnoreCase))
        {
            return new ValidationResult(true, []);
        }

        var error = parentTypes.Count > 0
            ? $"A {childType} needs a parent of type {string.Join(" or ", parentTypes)}, but the parent is a '{parentWorkItem.Type}'."
            : $"A {childType} cannot have a parent, but the parent is a '{parentWorkItem.Type}'.";

        return new ValidationResult(false, [error]);
    }
}
