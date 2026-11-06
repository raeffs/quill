using Quill.Core;
using Shouldly;

namespace Quill.Core.Tests;

public class PipelineParametersTests
{
    [Theory]
    [InlineData("env=dev", "env", "dev")]
    [InlineData("filter=a=b", "filter", "a=b")]
    [InlineData("empty=", "empty", "")]
    [InlineData("note=Bär & <x>", "note", "Bär & <x>")]
    [InlineData(" spaced = kept ", " spaced ", " kept ")]
    public void TrySplit_NameAndValue_SplitsAtFirstEquals(string token, string expectedName, string expectedValue)
    {
        // Act
        var result = PipelineParameters.TrySplit(token, out var name, out var value);

        // Assert
        result.ShouldBeTrue();
        name.ShouldBe(expectedName);
        value.ShouldBe(expectedValue);
    }

    [Theory]
    [InlineData("noequals")]
    [InlineData("=value")]
    [InlineData("")]
    public void TrySplit_NoEqualsOrEmptyName_ReturnsFalse(string token)
    {
        // Act
        var result = PipelineParameters.TrySplit(token, out var name, out var value);

        // Assert
        result.ShouldBeFalse();
        name.ShouldBe(string.Empty);
        value.ShouldBe(string.Empty);
    }
}
