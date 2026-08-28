using Shouldly;

namespace Quill.Cli.Tests;

public class PrCreateCommandParsingTests
{
    [Fact]
    public void Parse_RequiredFlagsOnly_LeavesTheRestUnset()
    {
        // Arrange + Act
        var parseResult = CliHost.Parse(
            ["pr", "create", "--repo", "importer", "--source-branch", "feat/x", "--title", "Add the importer"],
            TestServices.Empty);

        // Assert
        parseResult.Errors.ShouldBeEmpty();
        parseResult.GetValue<string?>("--repo").ShouldBe("importer");
        parseResult.GetValue<string?>("--source-branch").ShouldBe("feat/x");
        parseResult.GetValue<string?>("--title").ShouldBe("Add the importer");
        parseResult.GetValue<string?>("--target-branch").ShouldBeNull();
        parseResult.GetValue<string?>("--description-file").ShouldBeNull();
        parseResult.GetValue<int[]>("--work-item").ShouldBeEmpty();
    }

    [Theory]
    [InlineData("--repo")]
    [InlineData("--source-branch")]
    [InlineData("--title")]
    public void Parse_MissingRequiredFlag_ProducesError(string missing)
    {
        // Arrange
        var args = new List<string> { "pr", "create" };
        foreach (var flag in new[] { "--repo", "--source-branch", "--title" })
        {
            if (!string.Equals(flag, missing, StringComparison.Ordinal))
            {
                args.Add(flag);
                args.Add("value");
            }
        }

        // Act
        var parseResult = CliHost.Parse(args.ToArray(), TestServices.Empty);

        // Assert
        parseResult.Errors.ShouldNotBeEmpty();
    }

    [Fact]
    public void Parse_WorkItem_IsRepeatable()
    {
        // Arrange + Act
        var parseResult = CliHost.Parse(
            [
                "pr", "create",
                "--repo", "importer",
                "--source-branch", "feat/x",
                "--title", "Add the importer",
                "--work-item", "63480",
                "--work-item", "63481",
            ],
            TestServices.Empty);

        // Assert
        parseResult.Errors.ShouldBeEmpty();
        parseResult.GetValue<int[]>("--work-item").ShouldBe([63480, 63481]);
    }

    [Fact]
    public void Parse_TargetBranchAndDescriptionFile_AreRead()
    {
        // Arrange + Act
        var parseResult = CliHost.Parse(
            [
                "pr", "create",
                "--repo", "importer",
                "--source-branch", "feat/x",
                "--title", "Add the importer",
                "--target-branch", "develop",
                "--description-file", "-",
            ],
            TestServices.Empty);

        // Assert
        parseResult.Errors.ShouldBeEmpty();
        parseResult.GetValue<string?>("--target-branch").ShouldBe("develop");
        parseResult.GetValue<string?>("--description-file").ShouldBe("-");
    }
}
