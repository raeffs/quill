using Quill.Core.Models;
using Quill.Core.Validation;
using Quill.Tests.Shared;
using Shouldly;

namespace Quill.Core.Tests.Validation;

public class ParentValidatorTests
{
    [Theory]
    [InlineData("Task", "Product Backlog Item")]
    [InlineData("Task", "Bug")]
    [InlineData("Product Backlog Item", "Feature")]
    [InlineData("Bug", "Feature")]
    [InlineData("task", "product backlog item")]
    public void Validate_AllowedParentType_ReturnsSuccess(string childType, string parentType)
    {
        var result = ParentValidator.Validate(childType, Parent(parentType), TestConstants.TestUserId);

        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("Task", "Feature")]
    [InlineData("Task", "Task")]
    [InlineData("Product Backlog Item", "Product Backlog Item")]
    [InlineData("Product Backlog Item", "Bug")]
    [InlineData("Bug", "Task")]
    [InlineData("Feature", "Feature")]
    [InlineData("Epic", "Feature")]
    public void Validate_DisallowedParentType_ReturnsErrorNamingParentType(string childType, string parentType)
    {
        var result = ParentValidator.Validate(childType, Parent(parentType), TestConstants.TestUserId);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldHaveSingleItem().ShouldContain($"'{parentType}'");
    }

    [Theory]
    [InlineData("New")]
    [InlineData("Closed")]
    [InlineData("Removed")]
    public void Validate_ParentInAnyState_ReturnsSuccess(string state)
    {
        var result = ParentValidator.Validate("Task", Parent("Product Backlog Item", state), TestConstants.TestUserId);

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WrongAssignee_ReturnsError()
    {
        var result = ParentValidator.Validate("Task", Parent("Bug", assignedToId: "other-user-id"), TestConstants.TestUserId);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldHaveSingleItem().ShouldContain("assignee");
    }

    [Fact]
    public void Validate_AssigneeComparison_IsCaseInsensitive()
    {
        var parent = Parent("Bug", assignedToId: TestConstants.TestUserId.ToUpperInvariant());

        var result = ParentValidator.Validate("Task", parent, TestConstants.TestUserId);

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WrongTypeAndWrongAssignee_ReportsBoth()
    {
        var result = ParentValidator.Validate("Task", Parent("Feature", assignedToId: "other-user-id"), TestConstants.TestUserId);

        result.IsValid.ShouldBeFalse();
        result.Errors.Count.ShouldBe(2);
        result.Errors.ShouldContain(e => e.Contains("'Feature'"));
        result.Errors.ShouldContain(e => e.Contains("assignee"));
    }

    [Fact]
    public void ValidateType_IgnoresAssignee()
    {
        var result = ParentValidator.ValidateType("Task", Parent("Bug", assignedToId: "other-user-id"));

        result.IsValid.ShouldBeTrue();
    }

    private static WorkItem Parent(string type, string state = "New", string? assignedToId = null) => new()
    {
        Id = 100,
        Type = type,
        Title = "Parent",
        State = state,
        AssignedToId = assignedToId ?? TestConstants.TestUserId,
    };
}
