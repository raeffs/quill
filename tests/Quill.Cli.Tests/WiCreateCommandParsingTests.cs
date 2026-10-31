using Shouldly;

namespace Quill.Cli.Tests;

public class WiCreateCommandParsingTests
{
    [Fact]
    public void Parse_WiCreateTask_IsParseError()
    {
        CliHost.Parse(["wi", "create-task", "42", "Some title"], TestServices.Empty).Errors.ShouldNotBeEmpty();
    }

    [Fact]
    public void Parse_RequiredFlagsOnly_LeavesTheRestUnset()
    {
        // Arrange + Act
        var parseResult = CliHost.Parse(
            ["wi", "create", "--type", "Task", "--title", "Do it", "--parent", "42"],
            TestServices.Empty);

        // Assert
        parseResult.Errors.ShouldBeEmpty();
        parseResult.GetValue<string?>("--type").ShouldBe("Task");
        parseResult.GetValue<string?>("--title").ShouldBe("Do it");
        parseResult.GetValue<int>("--parent").ShouldBe(42);
        parseResult.GetValue<string?>("--description-file").ShouldBeNull();
        parseResult.GetValue<string?>("--assignee").ShouldBeNull();
    }

    [Theory]
    [InlineData("--type")]
    [InlineData("--title")]
    [InlineData("--parent")]
    public void Parse_MissingRequiredFlag_ProducesError(string missing)
    {
        // Arrange
        var args = new List<string> { "wi", "create" };
        foreach (var (flag, value) in new[] { ("--type", "Task"), ("--title", "t"), ("--parent", "42") })
        {
            if (!string.Equals(flag, missing, StringComparison.Ordinal))
            {
                args.Add(flag);
                args.Add(value);
            }
        }

        // Act
        var parseResult = CliHost.Parse(args.ToArray(), TestServices.Empty);

        // Assert
        parseResult.Errors.ShouldNotBeEmpty();
    }

    [Theory]
    [InlineData("task")]
    [InlineData("product backlog item")]
    [InlineData("BUG")]
    [InlineData("Feature")]
    [InlineData("feature")]
    public void Parse_TypeInAnyCase_IsAccepted(string type)
    {
        CliHost.Parse(Args(type), TestServices.Empty).Errors.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("Epic")]
    [InlineData("PBI")]
    public void Parse_TypeOutsideHierarchy_IsParseError(string type)
    {
        var parseResult = CliHost.Parse(Args(type), TestServices.Empty);

        parseResult.Errors.ShouldHaveSingleItem().Message.ShouldContain("Feature, Product Backlog Item, Bug, Task");
    }

    [Fact]
    public void Parse_DescriptionFileAndAssignee_AreRead()
    {
        // Arrange + Act
        var parseResult = CliHost.Parse(
            [.. Args("Task"), "--description-file", "-", "--assignee", "Jane Doe"],
            TestServices.Empty);

        // Assert
        parseResult.Errors.ShouldBeEmpty();
        parseResult.GetValue<string?>("--description-file").ShouldBe("-");
        parseResult.GetValue<string?>("--assignee").ShouldBe("Jane Doe");
    }

    [Fact]
    public async Task InvokeAsync_MissingType_Returns3()
    {
        var exitCode = await CliHost.InvokeAsync(["wi", "create", "--title", "t", "--parent", "42"], TestServices.Empty);

        exitCode.ShouldBe(3);
    }

    private static string[] Args(string type) => ["wi", "create", "--type", type, "--title", "t", "--parent", "42"];
}
