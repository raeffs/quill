using Quill.Core;
using Shouldly;

namespace Quill.Core.Tests;

public class ThreadStatusTests
{
    [Fact]
    public void All_HoldsTheSixValuesInOrder() =>
        ThreadStatus.All.ShouldBe(["active", "fixed", "wontFix", "closed", "byDesign", "pending"]);

    [Theory]
    [InlineData("active")]
    [InlineData("fixed")]
    [InlineData("wontFix")]
    [InlineData("closed")]
    [InlineData("byDesign")]
    [InlineData("pending")]
    public void IsKnown_OneOfTheSix_IsTrue(string value) =>
        ThreadStatus.IsKnown(value).ShouldBeTrue();

    [Theory]
    [InlineData("wontfix")]
    [InlineData("Fixed")]
    [InlineData("unknown")]
    [InlineData("")]
    [InlineData(" active")]
    public void IsKnown_AnyOtherValue_IsFalse(string value) =>
        ThreadStatus.IsKnown(value).ShouldBeFalse();
}
