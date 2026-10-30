using Shouldly;

namespace Quill.Cli.Tests;

public class ViewCommandParsingTests
{
    [Fact]
    public void Parse_WiViewMarkdown_IsParseError()
    {
        var parseResult = CliHost.Parse(["wi", "view", "42", "--markdown"], TestServices.Empty);

        parseResult.Errors.ShouldNotBeEmpty();
    }

    [Fact]
    public void Parse_WiViewWithChildren_Parses()
    {
        var parseResult = CliHost.Parse(["wi", "view", "42", "--with-children"], TestServices.Empty);

        parseResult.Errors.ShouldBeEmpty();
        parseResult.CommandResult.Command.Name.ShouldBe("view");
        parseResult.GetValue<int>("id").ShouldBe(42);
        parseResult.GetValue<bool>("--with-children").ShouldBeTrue();
    }
}
