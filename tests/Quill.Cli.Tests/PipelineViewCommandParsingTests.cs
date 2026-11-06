using Shouldly;

namespace Quill.Cli.Tests;

public class PipelineViewCommandParsingTests
{
    [Fact]
    public void Parse_RunId_NoErrors()
    {
        var parseResult = CliHost.Parse(["pipeline", "view", "121549"], TestServices.Empty);

        parseResult.Errors.ShouldBeEmpty();
        parseResult.GetValue<int>("runId").ShouldBe(121549);
    }

    [Fact]
    public void Parse_MissingRunId_ProducesError()
    {
        var parseResult = CliHost.Parse(["pipeline", "view"], TestServices.Empty);

        parseResult.Errors.ShouldNotBeEmpty();
    }

    [Fact]
    public void Parse_NonIntegerRunId_ProducesError()
    {
        var parseResult = CliHost.Parse(["pipeline", "view", "abc"], TestServices.Empty);

        parseResult.Errors.ShouldNotBeEmpty();
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    public void Parse_RunIdBelowOne_ProducesError(string runId)
    {
        var parseResult = CliHost.Parse(["pipeline", "view", runId], TestServices.Empty);

        parseResult.Errors.ShouldNotBeEmpty();
    }
}
