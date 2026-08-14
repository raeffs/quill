using Shouldly;

namespace Quill.Cli.Tests;

public class PrThreadsCommandParsingTests
{
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
}
