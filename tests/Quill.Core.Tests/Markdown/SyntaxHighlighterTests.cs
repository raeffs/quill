using Quill.Core.Markdown;
using Shouldly;

namespace Quill.Core.Tests.Markdown;

public class SyntaxHighlighterTests
{
    [Fact]
    public void Highlight_CSharpKeyword_AppliesMauveColor()
    {
        var result = SyntaxHighlighter.Highlight("public class Foo { }", "csharp");

        result.ShouldNotBeNull();
        result.ShouldContain("color: #ca9ee6;");
        result.ShouldContain("public");
    }

    [Fact]
    public void Highlight_CSharpString_AppliesGreenColor()
    {
        var result = SyntaxHighlighter.Highlight("var x = \"hello\";", "csharp");

        result.ShouldNotBeNull();
        result.ShouldContain("color: #a6d189;");
        result.ShouldContain("hello");
    }

    [Fact]
    public void Highlight_CSharpComment_AppliesOverlay0Color()
    {
        var result = SyntaxHighlighter.Highlight("// a comment", "csharp");

        result.ShouldNotBeNull();
        result.ShouldContain("color: #737994;");
        result.ShouldContain("a&nbsp;comment");
    }

    [Fact]
    public void Highlight_OutputStructure_HasOuterDivWithDataLang()
    {
        var result = SyntaxHighlighter.Highlight("var x = 1;", "csharp");

        result.ShouldNotBeNull();
        result.ShouldContain("data-lang=\"csharp\"");
        result.ShouldContain("background-color: #303446;");
        result.ShouldContain("color: #c6d0f5;");
    }

    [Fact]
    public void Highlight_MultipleLines_EachLineIsDiv()
    {
        var result = SyntaxHighlighter.Highlight("line1\nline2\nline3", "csharp");

        result.ShouldNotBeNull();

        // Outer div + 3 line divs = at minimum 4 <div occurrences.
        var divCount = result.Split("<div").Length - 1;
        divCount.ShouldBeGreaterThanOrEqualTo(4);
    }

    [Fact]
    public void Highlight_Spaces_ConvertedToNbsp()
    {
        var result = SyntaxHighlighter.Highlight("    indented", "csharp");

        result.ShouldNotBeNull();
        result.ShouldContain("&nbsp;");
        result.ShouldNotContain("    indented");
    }

    [Fact]
    public void Highlight_UnsupportedLanguage_ReturnsNull()
    {
        var result = SyntaxHighlighter.Highlight("some code", "brainfuck");

        result.ShouldBeNull();
    }

    [Fact]
    public void Highlight_SqlKeywords_AppliesMauveColor()
    {
        var result = SyntaxHighlighter.Highlight("SELECT * FROM users", "sql");

        result.ShouldNotBeNull();
        result.ShouldContain("data-lang=\"sql\"");
        result.ShouldContain("color: #ca9ee6;");
    }

    [Fact]
    public void Highlight_Json_HighlightsKeys()
    {
        var result = SyntaxHighlighter.Highlight("{\"name\": \"value\"}", "json");

        result.ShouldNotBeNull();
        result.ShouldContain("data-lang=\"json\"");
        result.ShouldContain("color: #8caaee;"); // Blue for JSON keys
    }

    [Fact]
    public void Highlight_TypeScript_Works()
    {
        var result = SyntaxHighlighter.Highlight("const x: string = \"hello\";", "typescript");

        result.ShouldNotBeNull();
        result.ShouldContain("data-lang=\"typescript\"");
    }

    [Theory]
    [InlineData("cs")]
    [InlineData("csharp")]
    public void Highlight_CSharpAliases_AllWork(string lang)
    {
        var result = SyntaxHighlighter.Highlight("public class Foo { }", lang);

        result.ShouldNotBeNull();
        result.ShouldContain("data-lang=\"" + lang + "\"");
    }

    [Theory]
    [InlineData("ts")]
    [InlineData("typescript")]
    public void Highlight_TypeScriptAliases_AllWork(string lang)
    {
        var result = SyntaxHighlighter.Highlight("const x = 1;", lang);

        result.ShouldNotBeNull();
        result.ShouldContain("data-lang=\"" + lang + "\"");
    }
}
