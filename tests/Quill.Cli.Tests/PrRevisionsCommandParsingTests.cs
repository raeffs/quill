using Shouldly;

namespace Quill.Cli.Tests;

public class PrRevisionsCommandParsingTests
{
    [Fact]
    public void Parse_OnlyId_Succeeds()
    {
        // Arrange + Act
        var parseResult = CliHost.Parse(["pr", "revisions", "4711"], TestServices.Empty);

        // Assert
        parseResult.Errors.ShouldBeEmpty();
        parseResult.GetValue<int>("id").ShouldBe(4711);
    }

    [Fact]
    public void Parse_MissingId_ProducesError()
    {
        // Arrange + Act
        var parseResult = CliHost.Parse(["pr", "revisions"], TestServices.Empty);

        // Assert
        parseResult.Errors.ShouldNotBeEmpty();
    }

    [Fact]
    public void Parse_AnyFlag_ProducesError()
    {
        // Arrange + Act
        var parseResult = CliHost.Parse(["pr", "revisions", "4711", "--limit", "5"], TestServices.Empty);

        // Assert
        parseResult.Errors.ShouldNotBeEmpty();
    }
}
