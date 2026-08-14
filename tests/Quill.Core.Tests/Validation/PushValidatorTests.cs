using Quill.Core.Models;
using Quill.Core.Validation;
using Quill.Tests.Shared;
using Shouldly;

namespace Quill.Core.Tests.Validation;

public class PushValidatorTests
{
    [Fact]
    public void Validate_ValidWorkItem_ReturnsSuccess()
    {
        var workItem = new WorkItem
        {
            Id = 1,
            Type = "Bug",
            Title = "Fix it",
            State = "Active",
            AssignedToId = TestConstants.TestUserId,
        };
        var config = new QuillConfig
        {
            ServerUrl = "https://x",
            Collection = "C",
            Project = "P",
            AllowedStates = ["Active", "New"],
            AllowedParentStates = ["New"],
        };

        var result = PushValidator.Validate(workItem, config, TestConstants.TestUserId);

        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }

    [Fact]
    public void Validate_WrongState_ReturnsError()
    {
        var workItem = new WorkItem
        {
            Id = 1,
            Type = "Bug",
            Title = "Fix it",
            State = "Closed",
            AssignedToId = TestConstants.TestUserId,
        };
        var config = new QuillConfig
        {
            ServerUrl = "https://x",
            Collection = "C",
            Project = "P",
            AllowedStates = ["Active", "New"],
            AllowedParentStates = ["New"],
        };

        var result = PushValidator.Validate(workItem, config, TestConstants.TestUserId);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.Contains("state"));
    }

    [Fact]
    public void Validate_WrongAssignee_ReturnsError()
    {
        var workItem = new WorkItem
        {
            Id = 1,
            Type = "Bug",
            Title = "Fix it",
            State = "Active",
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

        var result = PushValidator.Validate(workItem, config, TestConstants.TestUserId);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.Contains("assignee"));
    }

    [Fact]
    public void Validate_AssigneeComparison_IsCaseInsensitive()
    {
        var workItem = new WorkItem
        {
            Id = 1,
            Type = "Bug",
            Title = "Fix it",
            State = "Active",
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

        var result = PushValidator.Validate(workItem, config, TestConstants.TestUserId);

        result.IsValid.ShouldBeTrue();
    }
}
