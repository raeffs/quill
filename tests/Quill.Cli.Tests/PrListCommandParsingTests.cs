using Shouldly;

namespace Quill.Cli.Tests;

public class PrListCommandParsingTests
{
    [Fact]
    public void Parse_NoReviewer_DefaultsToAtMe()
    {
        // Arrange + Act
        var parseResult = CliHost.Parse(["pr", "list"], TestServices.Empty);

        // Assert
        parseResult.Errors.ShouldBeEmpty();
        parseResult.GetValue<string?>("--reviewer").ShouldBe("@me");
    }

    [Fact]
    public void Parse_NoState_DefaultsToActive()
    {
        // Arrange + Act
        var parseResult = CliHost.Parse(["pr", "list"], TestServices.Empty);

        // Assert
        parseResult.Errors.ShouldBeEmpty();
        parseResult.GetValue<string?>("--state").ShouldBe("active");
    }

    [Fact]
    public void Parse_NoLimit_DefaultsTo50()
    {
        // Arrange + Act
        var parseResult = CliHost.Parse(["pr", "list"], TestServices.Empty);

        // Assert
        parseResult.Errors.ShouldBeEmpty();
        parseResult.GetValue<int>("--limit").ShouldBe(50);
    }

    [Fact]
    public void Parse_AllFlags_PassThrough()
    {
        // Arrange + Act
        var parseResult = CliHost.Parse(
            [
                "pr", "list",
                "--reviewer", "@me",
                "--author", "@me",
                "--state", "completed",
                "--repo", "importer",
                "--include-drafts",
                "--limit", "10",
            ],
            TestServices.Empty);

        // Assert
        parseResult.Errors.ShouldBeEmpty();
        parseResult.GetValue<string?>("--reviewer").ShouldBe("@me");
        parseResult.GetValue<string?>("--author").ShouldBe("@me");
        parseResult.GetValue<string?>("--state").ShouldBe("completed");
        parseResult.GetValue<string?>("--repo").ShouldBe("importer");
        parseResult.GetValue<bool>("--include-drafts").ShouldBeTrue();
        parseResult.GetValue<int>("--limit").ShouldBe(10);
    }

    [Fact]
    public void Parse_IncludeDrafts_DefaultsToFalse()
    {
        // Arrange + Act
        var parseResult = CliHost.Parse(["pr", "list"], TestServices.Empty);

        // Assert
        parseResult.GetValue<bool>("--include-drafts").ShouldBeFalse();
    }
}
