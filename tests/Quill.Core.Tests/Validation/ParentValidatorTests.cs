using Quill.Core.Models;
using Quill.Core.Validation;
using Quill.Tests.Shared;
using Shouldly;

namespace Quill.Core.Tests.Validation;

public class ParentValidatorTests
{
    [Fact]
    public void Validate_ValidParent_ReturnsSuccess()
    {
        var parent = new WorkItem
        {
            Id = 100,
            Type = "Product Backlog Item",
            Title = "Parent",
            State = "New",
            AssignedToId = TestConstants.TestUserId,
        };
        var config = new QuillConfig
        {
            ServerUrl = "https://x",
            Collection = "C",
            Project = "P",
            AllowedStates = ["Active"],
            AllowedParentStates = ["New", "Active"],
        };

        var result = ParentValidator.Validate(parent, config, TestConstants.TestUserId);

        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }

    [Fact]
    public void Validate_WrongState_ReturnsError()
    {
        var parent = new WorkItem
        {
            Id = 100,
            Type = "Product Backlog Item",
            Title = "Parent",
            State = "Closed",
            AssignedToId = TestConstants.TestUserId,
        };
        var config = new QuillConfig
        {
            ServerUrl = "https://x",
            Collection = "C",
            Project = "P",
            AllowedStates = ["Active"],
            AllowedParentStates = ["New"],
        };

        var result = ParentValidator.Validate(parent, config, TestConstants.TestUserId);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.Contains("state"));
    }

    [Fact]
    public void Validate_WrongAssignee_ReturnsError()
    {
        var parent = new WorkItem
        {
            Id = 100,
            Type = "Product Backlog Item",
            Title = "Parent",
            State = "New",
            AssignedToId = "other-user-id",
        };
        var config = new QuillConfig
        {
            ServerUrl = "https://x",
            Collection = "C",
            Project = "P",
            AllowedStates = ["Active"],
            AllowedParentStates = ["New"],
        };

        var result = ParentValidator.Validate(parent, config, TestConstants.TestUserId);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.Contains("assignee"));
    }

    [Fact]
    public void Validate_AssigneeComparison_IsCaseInsensitive()
    {
        var parent = new WorkItem
        {
            Id = 100,
            Type = "Product Backlog Item",
            Title = "Parent",
            State = "New",
            AssignedToId = TestConstants.TestUserId.ToUpperInvariant(),
        };
        var config = new QuillConfig
        {
            ServerUrl = "https://x",
            Collection = "C",
            Project = "P",
            AllowedStates = ["Active"],
            AllowedParentStates = ["New"],
        };

        var result = ParentValidator.Validate(parent, config, TestConstants.TestUserId);

        result.IsValid.ShouldBeTrue();
    }
}
