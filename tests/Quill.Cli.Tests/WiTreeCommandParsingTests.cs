using Shouldly;

namespace Quill.Cli.Tests;

public class WiTreeCommandParsingTests
{
    [Fact]
    public void Parse_DepthBelowOneWithoutAll_IsParseError()
    {
        var parseResult = CliHost.Parse(["wi", "tree", "5", "--depth", "0"], TestServices.Empty);

        parseResult.Errors.Select(e => e.Message).ShouldContain("--depth must be at least 1.", StringComparer.Ordinal);
    }

    [Fact]
    public void Parse_DepthBelowOneWithAll_Parses()
    {
        var parseResult = CliHost.Parse(["wi", "tree", "5", "--depth", "0", "--all"], TestServices.Empty);

        parseResult.Errors.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("abc")]
    [InlineData]
    public void Parse_DepthNotAnInteger_IsParseErrorWithoutThrowing(params string[] depthArgs)
    {
        string[] args = ["wi", "tree", "5", "--depth", .. depthArgs];

        var parseResult = CliHost.Parse(args, TestServices.Empty);

        parseResult.Errors.ShouldNotBeEmpty();
    }
}
