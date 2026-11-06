using Shouldly;

namespace Quill.Cli.Tests;

public class PipelineRunsCommandParsingTests
{
    [Fact]
    public void Parse_Bare_NoErrorsAndLimitDefaultsTo50()
    {
        // Arrange + Act
        var parseResult = CliHost.Parse(["pipeline", "runs"], TestServices.Empty);

        // Assert
        parseResult.Errors.ShouldBeEmpty();
        parseResult.GetValue<int>("--limit").ShouldBe(50);
        parseResult.GetValue<int?>("--pipeline").ShouldBeNull();
        parseResult.GetValue<string?>("--branch").ShouldBeNull();
        parseResult.GetValue<string?>("--status").ShouldBeNull();
        parseResult.GetValue<string?>("--result").ShouldBeNull();
        parseResult.GetValue<string?>("--reason").ShouldBeNull();
    }

    [Fact]
    public void Parse_AllFlags_NoErrors()
    {
        // Arrange + Act
        var parseResult = CliHost.Parse(
            ["pipeline", "runs", "--pipeline", "42", "--branch", "main", "--status", "completed", "--result", "failed", "--reason", "pullRequest", "--limit", "10"],
            TestServices.Empty);

        // Assert
        parseResult.Errors.ShouldBeEmpty();
        parseResult.GetValue<int?>("--pipeline").ShouldBe(42);
        parseResult.GetValue<string?>("--branch").ShouldBe("main");
        parseResult.GetValue<string?>("--status").ShouldBe("completed");
        parseResult.GetValue<string?>("--result").ShouldBe("failed");
        parseResult.GetValue<string?>("--reason").ShouldBe("pullRequest");
        parseResult.GetValue<int>("--limit").ShouldBe(10);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-5")]
    [InlineData("abc")]
    public void Parse_LimitNotAPositiveInteger_ProducesError(string limit)
    {
        // Arrange + Act
        var parseResult = CliHost.Parse(["pipeline", "runs", "--limit", limit], TestServices.Empty);

        // Assert
        parseResult.Errors.ShouldNotBeEmpty();
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("abc")]
    public void Parse_PipelineNotAPositiveInteger_ProducesError(string pipeline)
    {
        // Arrange + Act
        var parseResult = CliHost.Parse(["pipeline", "runs", "--pipeline", pipeline], TestServices.Empty);

        // Assert
        parseResult.Errors.ShouldNotBeEmpty();
    }

    [Fact]
    public void Parse_Skip_ProducesError()
    {
        // Arrange + Act
        var parseResult = CliHost.Parse(["pipeline", "runs", "--skip", "10"], TestServices.Empty);

        // Assert
        parseResult.Errors.ShouldNotBeEmpty();
    }
}
