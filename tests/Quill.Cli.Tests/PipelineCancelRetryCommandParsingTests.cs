using Shouldly;

namespace Quill.Cli.Tests;

public class PipelineCancelRetryCommandParsingTests
{
    [Theory]
    [InlineData("cancel")]
    [InlineData("retry")]
    public void Parse_RunId_NoErrors(string command)
    {
        var parseResult = CliHost.Parse(["pipeline", command, "4202"], TestServices.Empty);

        parseResult.Errors.ShouldBeEmpty();
        parseResult.GetValue<int>("runId").ShouldBe(4202);
    }

    [Theory]
    [InlineData("cancel")]
    [InlineData("cancel", "abc")]
    [InlineData("cancel", "0")]
    [InlineData("cancel", "4202", "--stage", "Build")]
    [InlineData("cancel", "4202", "--with-stages")]
    [InlineData("retry")]
    [InlineData("retry", "abc")]
    [InlineData("retry", "-1")]
    [InlineData("retry", "4202", "--stage", "Build")]
    [InlineData("retry", "4202", "4203")]
    public void Parse_BadInput_ProducesError(params string[] args)
    {
        var parseResult = CliHost.Parse(["pipeline", .. args], TestServices.Empty);

        parseResult.Errors.ShouldNotBeEmpty();
    }
}
