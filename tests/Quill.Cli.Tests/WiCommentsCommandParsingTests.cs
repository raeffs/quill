using Shouldly;

namespace Quill.Cli.Tests;

public class WiCommentsCommandParsingTests
{
    [Fact]
    public void Parse_LimitZero_IsParseError()
    {
        var parseResult = CliHost.Parse(["wi", "comments", "5", "--limit", "0"], TestServices.Empty);

        parseResult.Errors.Select(e => e.Message).ShouldContain("--limit must be a positive integer.", StringComparer.Ordinal);
    }
}
