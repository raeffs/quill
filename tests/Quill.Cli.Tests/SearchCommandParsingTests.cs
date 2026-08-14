using System.CommandLine;
using System.CommandLine.Parsing;
using Quill.Cli;
using Shouldly;

namespace Quill.Cli.Tests;

public class SearchCommandParsingTests
{
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

    private static void AssertAssigneeParsesAs(string input)
    {
        var parseResult = CliHost.Parse(["wi", "search", "--assignee", input], TestServices.Empty);

        parseResult.Errors.ShouldBeEmpty();

        var optionResult = (OptionResult?)parseResult.GetResult("--assignee");
        optionResult.ShouldNotBeNull();
        optionResult.Tokens.Count.ShouldBe(1);
        optionResult.Tokens[0].Value.ShouldBe(input);
    }
}
