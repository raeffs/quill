using Shouldly;

namespace Quill.Cli.Tests;

public class PrThreadsCommandParsingTests
{
    private const string StatusMessage = "--status must be one of: active, fixed, wontFix, closed, byDesign, pending.";

    [Fact]
    public void Parse_OnlyId_NoStatusOrLimit()
    {
        // Arrange + Act
        var parseResult = CliHost.Parse(["pr", "threads", "4711"], TestServices.Empty);

        // Assert
        parseResult.Errors.ShouldBeEmpty();
        parseResult.GetValue<int>("id").ShouldBe(4711);
        parseResult.GetValue<string[]>("--status").ShouldBeEmpty();
        parseResult.GetValue<int?>("--limit").ShouldBeNull();
    }

    [Fact]
    public void Parse_MultiStatus_CollectsAllValues()
    {
        // Arrange + Act
        var parseResult = CliHost.Parse(
            ["pr", "threads", "4711", "--status", "active", "--status", "pending"],
            TestServices.Empty);

        // Assert
        parseResult.Errors.ShouldBeEmpty();
        parseResult.GetValue<string[]>("--status").ShouldBe(["active", "pending"]);
    }

    [Fact]
    public void Parse_Limit_ParsesAsInt()
    {
        // Arrange + Act
        var parseResult = CliHost.Parse(
            ["pr", "threads", "4711", "--limit", "10"],
            TestServices.Empty);

        // Assert
        parseResult.Errors.ShouldBeEmpty();
        parseResult.GetValue<int?>("--limit").ShouldBe(10);
    }

    [Fact]
    public void Parse_MissingId_ProducesError()
    {
        // Arrange + Act
        var parseResult = CliHost.Parse(["pr", "threads"], TestServices.Empty);

        // Assert
        parseResult.Errors.ShouldNotBeEmpty();
    }

    [Fact]
    public void Parse_LimitZero_IsParseError()
    {
        // Arrange + Act
        var parseResult = CliHost.Parse(["pr", "threads", "12", "--limit", "0"], TestServices.Empty);

        // Assert
        parseResult.Errors.Select(e => e.Message).ShouldContain("--limit must be a positive integer.", StringComparer.Ordinal);
    }

    [Theory]
    [InlineData("active")]
    [InlineData("fixed")]
    [InlineData("wontFix")]
    [InlineData("closed")]
    [InlineData("byDesign")]
    [InlineData("pending")]
    public void Parse_KnownStatus_ProducesNoError(string status)
    {
        var parseResult = CliHost.Parse(["pr", "threads", "4711", "--status", status], TestServices.Empty);

        parseResult.Errors.ShouldBeEmpty();
        parseResult.GetValue<string[]>("--status").ShouldBe([status]);
    }

    [Theory]
    [InlineData("--status", "wontfix")]
    [InlineData("--status", "Fixed")]
    [InlineData("--status", "unknown")]
    [InlineData("--status", "")]
    [InlineData("--status=wontfix")]
    [InlineData("--status", "active", "--status", "wontfix")]
    public void Parse_UnknownStatus_ProducesTheMessage(params string[] flags)
    {
        var parseResult = CliHost.Parse(["pr", "threads", "4711", .. flags], TestServices.Empty);

        parseResult.Errors.Select(e => e.Message).ShouldContain(StatusMessage, StringComparer.Ordinal);
    }

    [Fact]
    public void StatusOption_Description_ListsTheSixValues()
    {
        var parseResult = CliHost.Parse(["pr", "threads", "4711"], TestServices.Empty);

        var option = parseResult.CommandResult.Command.Options.Single(o => string.Equals(o.Name, "--status", StringComparison.Ordinal));

        option.Description.ShouldNotBeNull().ShouldContain("active, fixed, wontFix, closed, byDesign, pending");
    }
}
