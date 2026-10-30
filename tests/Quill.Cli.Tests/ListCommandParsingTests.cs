using System.CommandLine;
using System.CommandLine.Parsing;
using Quill.Core;
using Shouldly;

namespace Quill.Cli.Tests;

public class ListCommandParsingTests
{
    [Fact]
    public void Parse_WiSearch_IsParseError()
    {
        CliHost.Parse(["wi", "search", "login"], TestServices.Empty).Errors.ShouldNotBeEmpty();
    }

    [Fact]
    public void Parse_Skip_IsParseError()
    {
        CliHost.Parse(["wi", "list", "--state", "Active", "--skip", "10"], TestServices.Empty).Errors.ShouldNotBeEmpty();
    }

    [Fact]
    public void Parse_NoAssignee_HasNoDefault()
    {
        var parseResult = CliHost.Parse(["wi", "list", "--state", "Active"], TestServices.Empty);

        parseResult.Errors.ShouldBeEmpty();
        parseResult.GetValue<string?>("--assignee").ShouldBeNull();
    }

    [Fact]
    public void Parse_ExplicitAssignee_PassesThrough()
    {
        var parseResult = CliHost.Parse(["wi", "list", "--assignee", "Jane Doe"], TestServices.Empty);

        parseResult.Errors.ShouldBeEmpty();
        parseResult.GetValue<string?>("--assignee").ShouldBe("Jane Doe");
    }

    [Fact]
    public void Parse_AssigneeAtMe_PreservesLiteralAtMeValue()
    {
        // Regression: System.CommandLine's default response-file token replacer
        // treats any `@<value>` argument as a file path, which would swallow `@me`.
        // CliHost disables that replacer; this test guards against its reintroduction.
        AssertAssigneeParsesAs("@me");
    }

    [Fact]
    public void Parse_AssigneeDisplayName_PreservesLiteralValue()
    {
        AssertAssigneeParsesAs("Jane Doe");
    }

    [Fact]
    public void Parse_Title_BuildsSameWiqlAsFormerSearchQuery()
    {
        var (wiql, _) = BuildWiql(CliHost.Parse(["wi", "list", "--title", "login bug"], TestServices.Empty));

        wiql.ShouldBe(
            "SELECT [System.Id] FROM WorkItems WHERE " +
            "[System.Title] CONTAINS WORDS 'login bug' " +
            "ORDER BY [System.ChangedDate] DESC");
    }

    [Fact]
    public void Parse_StateOnly_SendsNoAssigneeClause()
    {
        var (wiql, _) = BuildWiql(CliHost.Parse(["wi", "list", "--state", "Active"], TestServices.Empty));

        wiql.ShouldNotContain("[System.AssignedTo]");
    }

    [Fact]
    public void Parse_NoFilter_ThrowsProvideAtLeastOneFilter()
    {
        var ex = Should.Throw<InvalidOperationException>(() =>
            BuildWiql(CliHost.Parse(["wi", "list"], TestServices.Empty)));

        ex.Message.ShouldBe("provide at least one filter");
    }

    [Fact]
    public void Parse_StateTypeLimit_PassThrough()
    {
        var parseResult = CliHost.Parse(
            ["wi", "list", "--state", "New", "--state", "Active", "--type", "Bug", "--limit", "10"],
            TestServices.Empty);

        parseResult.Errors.ShouldBeEmpty();
        parseResult.GetValue<string[]>("--state").ShouldBe(["New", "Active"]);
        parseResult.GetValue<string[]>("--type").ShouldBe(["Bug"]);
        parseResult.GetValue<int>("--limit").ShouldBe(10);
    }

    [Fact]
    public void Parse_NoLimit_DefaultsTo50()
    {
        var parseResult = CliHost.Parse(["wi", "list"], TestServices.Empty);

        parseResult.Errors.ShouldBeEmpty();
        parseResult.GetValue<int>("--limit").ShouldBe(50);
    }

    private static void AssertAssigneeParsesAs(string input)
    {
        var parseResult = CliHost.Parse(["wi", "list", "--assignee", input], TestServices.Empty);

        parseResult.Errors.ShouldBeEmpty();

        var optionResult = (OptionResult?)parseResult.GetResult("--assignee");
        optionResult.ShouldNotBeNull();
        optionResult.Tokens.Count.ShouldBe(1);
        optionResult.Tokens[0].Value.ShouldBe(input);
    }

    private static (string Wiql, int Top) BuildWiql(ParseResult parseResult)
    {
        return WiqlBuilder.Build(
            parseResult.GetValue<string?>("--title"),
            parseResult.GetValue<string?>("--assignee"),
            parseResult.GetValue<string[]>("--state") ?? [],
            parseResult.GetValue<string[]>("--type") ?? [],
            parseResult.GetValue<int>("--limit"));
    }
}
