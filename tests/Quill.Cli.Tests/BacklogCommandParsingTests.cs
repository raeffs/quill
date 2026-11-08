using Shouldly;

namespace Quill.Cli.Tests;

public class BacklogCommandParsingTests
{
    [Fact]
    public void Parse_BacklogPull_AcceptsIdAndFile()
    {
        var parseResult = CliHost.Parse(["backlog", "pull", "42", "out.md"], TestServices.Empty);

        parseResult.Errors.ShouldBeEmpty();
        parseResult.CommandResult.Command.Name.ShouldBe("pull");
        parseResult.GetValue<int>("work-item-id").ShouldBe(42);
        parseResult.GetValue<string>("file-path").ShouldBe("out.md");
    }

    [Fact]
    public void Parse_BacklogPush_AcceptsFile()
    {
        var parseResult = CliHost.Parse(["backlog", "push", "item.md"], TestServices.Empty);

        parseResult.Errors.ShouldBeEmpty();
        parseResult.CommandResult.Command.Name.ShouldBe("push");
        parseResult.GetValue<string>("file-path").ShouldBe("item.md");
    }

    [Fact]
    public void Parse_BacklogCreate_AcceptsAssignee()
    {
        var parseResult = CliHost.Parse(["backlog", "create", "new.md", "--assignee", "Jane Doe"], TestServices.Empty);

        parseResult.Errors.ShouldBeEmpty();
        parseResult.CommandResult.Command.Name.ShouldBe("create");
        parseResult.GetValue<string>("file-path").ShouldBe("new.md");
        parseResult.GetValue<string?>("--assignee").ShouldBe("Jane Doe");
    }

    [Fact]
    public void Parse_BacklogCreate_RejectsAssignedTo()
    {
        var parseResult = CliHost.Parse(["backlog", "create", "new.md", "--assigned-to", "Jane Doe"], TestServices.Empty);

        parseResult.Errors.ShouldNotBeEmpty();
    }

    [Theory]
    [InlineData("pull", "42", "out.md")]
    [InlineData("push", "item.md")]
    [InlineData("create", "new.md")]
    public void Parse_OldWiSpelling_IsParseError(string command, params string[] rest)
    {
        var parseResult = CliHost.Parse(["wi", command, .. rest], TestServices.Empty);

        parseResult.Errors.ShouldNotBeEmpty();
    }

    [Fact]
    public void Root_HasBacklogPipelinePrAndWiGroups()
    {
        var root = CliHost.BuildRootCommand(TestServices.Empty);

        root.Subcommands.Select(c => c.Name).ShouldBe(["backlog", "pipeline", "pr", "wi"], ignoreOrder: true);
    }
}
