using Shouldly;

namespace Quill.Cli.Tests;

public class PrListCommandParsingTests
{
    [Fact]
    public void Parse_NoReviewer_HasNoDefault()
    {
        // Arrange + Act
        var parseResult = CliHost.Parse(["pr", "list"], TestServices.Empty);

        // Assert
        parseResult.Errors.ShouldBeEmpty();
        parseResult.GetValue<string?>("--reviewer").ShouldBeNull();
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
                "--limit", "10",
            ],
            TestServices.Empty);

        // Assert
        parseResult.Errors.ShouldBeEmpty();
        parseResult.GetValue<string?>("--reviewer").ShouldBe("@me");
        parseResult.GetValue<string?>("--author").ShouldBe("@me");
        parseResult.GetValue<string?>("--state").ShouldBe("completed");
        parseResult.GetValue<string?>("--repo").ShouldBe("importer");
        parseResult.GetValue<int>("--limit").ShouldBe(10);
    }

    [Fact]
    public void Parse_BranchFlagsAndSkip_PassThrough()
    {
        // Arrange + Act
        var parseResult = CliHost.Parse(
            [
                "pr", "list",
                "--source-branch", "feat/retry",
                "--target-branch", "main",
                "--skip", "100",
            ],
            TestServices.Empty);

        // Assert
        parseResult.Errors.ShouldBeEmpty();
        parseResult.GetValue<string?>("--source-branch").ShouldBe("feat/retry");
        parseResult.GetValue<string?>("--target-branch").ShouldBe("main");
        parseResult.GetValue<int?>("--skip").ShouldBe(100);
    }

    [Fact]
    public void Parse_NoBranchFlagsOrSkip_LeavesThemUnset()
    {
        // Arrange + Act
        var parseResult = CliHost.Parse(["pr", "list"], TestServices.Empty);

        // Assert
        parseResult.Errors.ShouldBeEmpty();
        parseResult.GetValue<string?>("--source-branch").ShouldBeNull();
        parseResult.GetValue<string?>("--target-branch").ShouldBeNull();
        parseResult.GetValue<int?>("--skip").ShouldBeNull();
    }

    [Fact]
    public void Parse_IncludeDrafts_IsNotAnOption()
    {
        // Arrange + Act
        var parseResult = CliHost.Parse(["pr", "list", "--include-drafts"], TestServices.Empty);

        // Assert
        parseResult.Errors.ShouldNotBeEmpty();
    }
}
