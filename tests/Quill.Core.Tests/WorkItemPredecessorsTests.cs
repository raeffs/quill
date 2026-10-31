using Quill.Core.Models;
using Shouldly;

namespace Quill.Core.Tests;

public class WorkItemPredecessorsTests
{
    private static readonly WorkItemPredecessor[] Current = [new(2, 1), new(3, 4)];

    [Fact]
    public void Validate_PositiveDistinctIds_ReturnsThemInOrder()
    {
        WorkItemPredecessors.Validate([3, 1]).ShouldBe([3, 1]);
        WorkItemPredecessors.Validate([]).ShouldBeEmpty();
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(-1, -1)]
    [InlineData(1, 1, 1)]
    public void Validate_InvalidIds_Throw(int named, params int[] ids)
    {
        var ex = Should.Throw<InvalidOperationException>(() => WorkItemPredecessors.Validate(ids));

        ex.Message.ShouldContain(named.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    [Fact]
    public void Diff_AddsAndRemoves()
    {
        var change = WorkItemPredecessors.Diff(Current, [1, 2]);

        change.Add.ShouldBe([1]);
        change.RemoveRelationIndexes.ShouldBe([4]);
        change.HasChanges.ShouldBeTrue();
    }

    [Fact]
    public void Diff_EmptyDesired_RemovesAllDescending()
    {
        var change = WorkItemPredecessors.Diff(Current, []);

        change.Add.ShouldBeEmpty();
        change.RemoveRelationIndexes.ShouldBe([4, 1]);
    }

    [Fact]
    public void Diff_NullDesired_HasNoChanges()
    {
        WorkItemPredecessors.Diff(Current, null).HasChanges.ShouldBeFalse();
    }

    [Fact]
    public void Diff_SameSetInOtherOrder_HasNoChanges()
    {
        WorkItemPredecessors.Diff(Current, [3, 2]).HasChanges.ShouldBeFalse();
    }
}
