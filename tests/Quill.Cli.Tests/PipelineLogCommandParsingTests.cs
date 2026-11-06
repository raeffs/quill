using Shouldly;

namespace Quill.Cli.Tests;

public class PipelineLogCommandParsingTests
{
    private static readonly string[] Prefix = ["pipeline", "log", "4201", "12"];

    [Fact]
    public void Parse_RunIdAndLogId_NoErrors()
    {
        var parseResult = CliHost.Parse(Prefix, TestServices.Empty);

        parseResult.Errors.ShouldBeEmpty();
        parseResult.GetValue<int>("runId").ShouldBe(4201);
        parseResult.GetValue<int>("logId").ShouldBe(12);
        parseResult.GetValue<int?>("--start-line").ShouldBeNull();
        parseResult.GetValue<int?>("--end-line").ShouldBeNull();
        parseResult.GetValue<int?>("--tail").ShouldBeNull();
    }

    [Fact]
    public void Parse_Range_NoErrors()
    {
        var parseResult = CliHost.Parse([.. Prefix, "--start-line", "401", "--end-line", "600"], TestServices.Empty);

        parseResult.Errors.ShouldBeEmpty();
        parseResult.GetValue<int?>("--start-line").ShouldBe(401);
        parseResult.GetValue<int?>("--end-line").ShouldBe(600);
    }

    [Fact]
    public void Parse_EqualStartAndEnd_NoErrors()
    {
        var parseResult = CliHost.Parse([.. Prefix, "--start-line", "5", "--end-line", "5"], TestServices.Empty);

        parseResult.Errors.ShouldBeEmpty();
    }

    [Fact]
    public void Parse_StartLineOnly_NoErrors()
    {
        var parseResult = CliHost.Parse([.. Prefix, "--start-line", "5"], TestServices.Empty);

        parseResult.Errors.ShouldBeEmpty();
        parseResult.GetValue<int?>("--start-line").ShouldBe(5);
    }

    [Fact]
    public void Parse_EndLineOnly_NoErrors()
    {
        var parseResult = CliHost.Parse([.. Prefix, "--end-line", "9"], TestServices.Empty);

        parseResult.Errors.ShouldBeEmpty();
        parseResult.GetValue<int?>("--end-line").ShouldBe(9);
    }

    [Fact]
    public void Parse_Tail_NoErrors()
    {
        var parseResult = CliHost.Parse([.. Prefix, "--tail", "200"], TestServices.Empty);

        parseResult.Errors.ShouldBeEmpty();
        parseResult.GetValue<int?>("--tail").ShouldBe(200);
    }

    [Theory]
    [InlineData("pipeline", "log", "4201")]
    [InlineData("pipeline", "log", "abc", "12")]
    [InlineData("pipeline", "log", "4201", "abc")]
    [InlineData("pipeline", "log", "0", "12")]
    [InlineData("pipeline", "log", "4201", "0")]
    [InlineData("pipeline", "log", "4201", "-1")]
    public void Parse_BadPositionals_ProducesError(params string[] args)
    {
        var parseResult = CliHost.Parse(args, TestServices.Empty);

        parseResult.Errors.ShouldNotBeEmpty();
    }

    [Theory]
    [InlineData("--start-line", "0")]
    [InlineData("--start-line", "-3")]
    [InlineData("--tail", "0")]
    [InlineData("--tail", "-1")]
    [InlineData("--end-line", "0")]
    [InlineData("--start-line", "10", "--end-line", "9")]
    [InlineData("--tail", "5", "--start-line", "1")]
    [InlineData("--tail", "5", "--end-line", "9")]
    public void Parse_BadFlags_ProducesError(params string[] flags)
    {
        var parseResult = CliHost.Parse([.. Prefix, .. flags], TestServices.Empty);

        parseResult.Errors.ShouldNotBeEmpty();
    }
}
