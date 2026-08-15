using Shouldly;

namespace Quill.Cli.Tests;

public class PrViewCommandParsingTests
{
    [Fact]
    public void Parse_OnlyId_NoFlags()
    {
        // Arrange + Act
        var parseResult = CliHost.Parse(["pr", "view", "4711"], TestServices.Empty);

        // Assert
        parseResult.Errors.ShouldBeEmpty();
        parseResult.GetValue<int>("id").ShouldBe(4711);
        parseResult.GetValue<bool>("--with-threads").ShouldBeFalse();
    }

    [Fact]
    public void Parse_WithThreads_SetsTrue()
    {
        // Arrange + Act
        var parseResult = CliHost.Parse(
            ["pr", "view", "4711", "--with-threads"], TestServices.Empty);

        // Assert
        parseResult.Errors.ShouldBeEmpty();
        parseResult.GetValue<bool>("--with-threads").ShouldBeTrue();
    }

    [Fact]
    public void Parse_MissingId_ProducesError()
    {
        // Arrange + Act
        var parseResult = CliHost.Parse(["pr", "view"], TestServices.Empty);

        // Assert
        parseResult.Errors.ShouldNotBeEmpty();
    }
}
