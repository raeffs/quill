using Shouldly;

namespace Quill.Cli.Tests;

public class PrChecksCommandParsingTests
{
    [Fact]
    public void Parse_Id_NoErrors()
    {
        var parseResult = CliHost.Parse(["pr", "checks", "4711"], TestServices.Empty);

        parseResult.Errors.ShouldBeEmpty();
        parseResult.GetValue<int>("id").ShouldBe(4711);
    }

    [Theory]
    [InlineData("pr", "checks")]
    [InlineData("pr", "checks", "abc")]
    [InlineData("pr", "checks", "0")]
    [InlineData("pr", "checks", "-1")]
    [InlineData("pr", "checks", "4711", "--with-threads")]
    public void Parse_BadInput_ProducesError(params string[] args)
    {
        var parseResult = CliHost.Parse(args, TestServices.Empty);

        parseResult.Errors.ShouldNotBeEmpty();
    }
}
