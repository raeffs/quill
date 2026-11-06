using Shouldly;

namespace Quill.Cli.Tests;

public class PipelineListCommandParsingTests
{
    [Fact]
    public void Parse_Bare_NoErrorsAndLimitDefaultsTo50()
    {
        // Arrange + Act
        var parseResult = CliHost.Parse(["pipeline", "list"], TestServices.Empty);

        // Assert
        parseResult.Errors.ShouldBeEmpty();
        parseResult.GetValue<int>("--limit").ShouldBe(50);
        parseResult.GetValue<string?>("--name").ShouldBeNull();
        parseResult.GetValue<string?>("--folder").ShouldBeNull();
    }

    [Fact]
    public void Parse_AllFlags_NoErrors()
    {
        // Arrange + Act
        var parseResult = CliHost.Parse(
            ["pipeline", "list", "--name", "G6*", "--folder", "\\Team A", "--limit", "10"],
            TestServices.Empty);

        // Assert
        parseResult.Errors.ShouldBeEmpty();
        parseResult.GetValue<string?>("--name").ShouldBe("G6*");
        parseResult.GetValue<string?>("--folder").ShouldBe("\\Team A");
        parseResult.GetValue<int>("--limit").ShouldBe(10);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-5")]
    [InlineData("abc")]
    public void Parse_LimitNotAPositiveInteger_ProducesError(string limit)
    {
        // Arrange + Act
        var parseResult = CliHost.Parse(["pipeline", "list", "--limit", limit], TestServices.Empty);

        // Assert
        parseResult.Errors.ShouldNotBeEmpty();
    }

    [Fact]
    public void Parse_Skip_ProducesError()
    {
        // Arrange + Act
        var parseResult = CliHost.Parse(["pipeline", "list", "--skip", "10"], TestServices.Empty);

        // Assert
        parseResult.Errors.ShouldNotBeEmpty();
    }
}
