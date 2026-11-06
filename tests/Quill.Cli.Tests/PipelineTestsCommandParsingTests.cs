using Shouldly;

namespace Quill.Cli.Tests;

public class PipelineTestsCommandParsingTests
{
    private static readonly string[] Prefix = ["pipeline", "tests", "4201"];

    [Fact]
    public void Parse_RunIdOnly_NoErrors()
    {
        var parseResult = CliHost.Parse(Prefix, TestServices.Empty);

        parseResult.Errors.ShouldBeEmpty();
        parseResult.GetValue<int>("runId").ShouldBe(4201);
        parseResult.GetValue<bool>("--with-results").ShouldBeFalse();
        parseResult.GetValue<string[]>("--outcome").ShouldBe(["Failed"]);
        parseResult.GetValue<int?>("--limit").ShouldBeNull();
        parseResult.GetValue<int?>("--skip").ShouldBeNull();
    }

    [Fact]
    public void Parse_WithResultsAndEveryFlag_NoErrors()
    {
        var parseResult = CliHost.Parse(
            [.. Prefix, "--with-results", "--outcome", "Passed", "--outcome", "NotExecuted", "--limit", "100", "--skip", "0"],
            TestServices.Empty);

        parseResult.Errors.ShouldBeEmpty();
        parseResult.GetValue<bool>("--with-results").ShouldBeTrue();
        parseResult.GetValue<string[]>("--outcome").ShouldBe(["Passed", "NotExecuted"]);
        parseResult.GetValue<int?>("--limit").ShouldBe(100);
        parseResult.GetValue<int?>("--skip").ShouldBe(0);
    }

    [Theory]
    [InlineData("--outcome", "Failed")]
    [InlineData("--limit", "5")]
    [InlineData("--skip", "0")]
    public void Parse_ResultFlagWithoutWithResults_ProducesError(params string[] flags)
    {
        var parseResult = CliHost.Parse([.. Prefix, .. flags], TestServices.Empty);

        parseResult.Errors.ShouldNotBeEmpty();
        parseResult.Errors.ShouldContain(error => error.Message == "--outcome, --limit and --skip need --with-results.");
    }

    [Theory]
    [InlineData("pipeline", "tests")]
    [InlineData("pipeline", "tests", "abc")]
    [InlineData("pipeline", "tests", "0")]
    [InlineData("pipeline", "tests", "4201", "--with-results", "--limit", "0")]
    [InlineData("pipeline", "tests", "4201", "--with-results", "--skip", "-1")]
    [InlineData("pipeline", "tests", "4201", "--with-results", "--limit", "abc")]
    public void Parse_BadInput_ProducesError(params string[] args)
    {
        var parseResult = CliHost.Parse(args, TestServices.Empty);

        parseResult.Errors.ShouldNotBeEmpty();
    }
}
