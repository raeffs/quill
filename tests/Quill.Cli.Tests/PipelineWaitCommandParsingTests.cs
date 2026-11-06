using Shouldly;

namespace Quill.Cli.Tests;

public class PipelineWaitCommandParsingTests
{
    [Fact]
    public void Parse_RunIdOnly_TimeoutDefaultsTo600()
    {
        var parseResult = CliHost.Parse(["pipeline", "wait", "4202"], TestServices.Empty);

        parseResult.Errors.ShouldBeEmpty();
        parseResult.GetValue<int>("runId").ShouldBe(4202);
        parseResult.GetValue<int>("--timeout").ShouldBe(600);
    }

    [Fact]
    public void Parse_Timeout_NoErrors()
    {
        var parseResult = CliHost.Parse(["pipeline", "wait", "4202", "--timeout", "30"], TestServices.Empty);

        parseResult.Errors.ShouldBeEmpty();
        parseResult.GetValue<int>("--timeout").ShouldBe(30);
    }

    [Theory]
    [InlineData("pipeline", "wait")]
    [InlineData("pipeline", "wait", "abc")]
    [InlineData("pipeline", "wait", "0")]
    [InlineData("pipeline", "wait", "4202", "--timeout", "0")]
    [InlineData("pipeline", "wait", "4202", "--timeout", "-5")]
    [InlineData("pipeline", "wait", "4202", "--timeout", "abc")]
    [InlineData("pipeline", "wait", "4202", "--interval", "5")]
    [InlineData("pipeline", "wait", "4202", "--with-stages")]
    public void Parse_BadInput_ProducesError(params string[] args)
    {
        var parseResult = CliHost.Parse(args, TestServices.Empty);

        parseResult.Errors.ShouldNotBeEmpty();
    }
}
