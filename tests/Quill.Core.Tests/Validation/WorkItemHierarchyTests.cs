using Quill.Core.Validation;
using Shouldly;

namespace Quill.Core.Tests.Validation;

public class WorkItemHierarchyTests
{
    [Fact]
    public void ChildTypes_AreThePbiBugAndTask()
    {
        WorkItemHierarchy.ChildTypes.ShouldBe(["Product Backlog Item", "Bug", "Task"]);
    }

    [Theory]
    [InlineData("Task", new[] { "Product Backlog Item", "Bug" })]
    [InlineData("Product Backlog Item", new[] { "Feature" })]
    [InlineData("bug", new[] { "Feature" })]
    [InlineData("Feature", new string[0])]
    [InlineData("Epic", new string[0])]
    public void ParentTypesOf_ReturnsTheFixedHierarchy(string childType, string[] expected)
    {
        WorkItemHierarchy.ParentTypesOf(childType).ShouldBe(expected, ignoreOrder: true);
    }
}
