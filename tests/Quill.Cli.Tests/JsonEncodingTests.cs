using System.Text.Json;
using Quill.Core.Models;
using Shouldly;

namespace Quill.Cli.Tests;

public class JsonEncodingTests
{
    [Fact]
    public void ErrorResult_DoesNotEscapeHtmlOrNonAscii()
    {
        var json = JsonSerializer.Serialize(
            new ErrorResult { Error = "<b> & \"Grüße\" — ü", Code = 1 },
            CommandHelpers.Context.ErrorResult);

        json.ShouldContain("<b>");
        json.ShouldContain("&");
        json.ShouldContain("Grüße");
        json.ShouldContain("—");
        json.ShouldNotContain("\\u");
    }

    [Fact]
    public void ErrorResult_IsNotIndented()
    {
        var json = JsonSerializer.Serialize(
            new ErrorResult { Error = "x", Code = 1 },
            CommandHelpers.Context.ErrorResult);

        json.ShouldNotContain("\n");
    }
}
