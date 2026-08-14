using Quill.Core;
using Shouldly;

namespace Quill.Cli.Tests;

public class ListCommandParsingTests
{
    [Fact]
    public void Parse_NoAssignee_DefaultsToAtMe()
    {
        var parseResult = CliHost.Parse(["wi", "list"], TestServices.Empty);

        parseResult.Errors.ShouldBeEmpty();
        parseResult.GetValue<string?>("--assignee").ShouldBe("@me");
    }

    [Fact]
    public void Parse_ExplicitAssignee_OverridesDefault()
    {
        var parseResult = CliHost.Parse(["wi", "list", "--assignee", "Jane Doe"], TestServices.Empty);

        parseResult.Errors.ShouldBeEmpty();
        parseResult.GetValue<string?>("--assignee").ShouldBe("Jane Doe");
    }

    [Fact]
    public void Parse_OmittedAssignee_BuildsSameWiqlAsExplicitAtMe()
    {
        var implicitResult = CliHost.Parse(["wi", "list"], TestServices.Empty);
        var explicitResult = CliHost.Parse(["wi", "list", "--assignee", "@me"], TestServices.Empty);

        var implicitBuild = WiqlBuilder.Build(
            null,
            implicitResult.GetValue<string?>("--assignee"),
            implicitResult.GetValue<string[]>("--state") ?? [],
            implicitResult.GetValue<string[]>("--type") ?? [],
            implicitResult.GetValue<int>("--limit"));

        var explicitBuild = WiqlBuilder.Build(
            null,
            explicitResult.GetValue<string?>("--assignee"),
            explicitResult.GetValue<string[]>("--state") ?? [],
            explicitResult.GetValue<string[]>("--type") ?? [],
            explicitResult.GetValue<int>("--limit"));

        implicitBuild.ShouldBe(explicitBuild);
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
}
