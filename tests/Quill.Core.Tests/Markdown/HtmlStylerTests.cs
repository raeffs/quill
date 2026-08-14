using Quill.Core.Markdown;
using Shouldly;

namespace Quill.Core.Tests.Markdown;

public class HtmlStylerTests
{
    [Fact]
    public void ApplyStyles_Table_AddsInlineStyles()
    {
        var html = "<table><thead><tr><th>H1</th><th>H2</th></tr></thead><tbody><tr><td>A</td><td>B</td></tr></tbody></table>";

        var result = HtmlStyler.ApplyStyles(html);

        result.ShouldContain("border-collapse: collapse");
        result.ShouldNotContain("width: 100%");
        result.ShouldContain("<th style=");
        result.ShouldContain("background-color: #f0f0f0");
        result.ShouldContain("border: 1px solid #ddd");
        result.ShouldContain("padding: 8px 16px");
        result.ShouldContain("font-weight: 600");
        result.ShouldContain("<td style=");
    }

    [Fact]
    public void ApplyStyles_PreCodeBlock_AddsInlineStyles()
    {
        var html = "<pre><code>var x = 1;</code></pre>";

        var result = HtmlStyler.ApplyStyles(html);

        result.ShouldContain("<pre style=");
        result.ShouldContain("background-color: #f5f5f5");
        result.ShouldContain("border: 1px solid #ddd");
        result.ShouldContain("border-radius: 4px");
        result.ShouldContain("padding: 12px");
        result.ShouldContain("font-family: monospace");
        result.ShouldContain("overflow-x: auto");
    }

    [Fact]
    public void ApplyStyles_InlineCode_AddsInlineStyles()
    {
        var html = "<p>Use <code>dotnet run</code> to start.</p>";

        var result = HtmlStyler.ApplyStyles(html);

        result.ShouldContain("<code style=");
        result.ShouldContain("background-color: #f5f5f5");
        result.ShouldContain("padding: 2px 6px");
        result.ShouldContain("border-radius: 3px");
    }

    [Fact]
    public void ApplyStyles_CodeInsidePre_DoesNotGetInlineCodeStyle()
    {
        var html = "<pre><code>var x = 1;</code></pre>";

        var result = HtmlStyler.ApplyStyles(html);

        result.ShouldNotContain("padding: 2px 6px");
    }

    [Fact]
    public void ApplyStyles_Blockquote_AddsInlineStyles()
    {
        var html = "<blockquote><p>Important note.</p></blockquote>";

        var result = HtmlStyler.ApplyStyles(html);

        result.ShouldContain("<blockquote style=");
        result.ShouldContain("border-left: 4px solid #0078d4");
        result.ShouldContain("padding: 8px 16px");
        result.ShouldContain("margin: 8px 0");
        result.ShouldContain("background-color: #f8f8f8");
    }

    [Fact]
    public void ApplyStyles_EmptyString_ReturnsEmpty()
    {
        var result = HtmlStyler.ApplyStyles(string.Empty);

        result.ShouldBeEmpty();
    }

    [Fact]
    public void ApplyStyles_NullString_ReturnsNull()
    {
        var result = HtmlStyler.ApplyStyles(null!);

        result.ShouldBeNull();
    }

    [Fact]
    public void ApplyStyles_StyledHtml_SurvivesSanitization()
    {
        var html = "<table><thead><tr><th>H</th></tr></thead><tbody><tr><td>V</td></tr></tbody></table>";
        var styled = HtmlStyler.ApplyStyles(html);

        var sanitizer = new Ganss.Xss.HtmlSanitizer();
        sanitizer.AllowedAttributes.Add("style");
        var sanitized = sanitizer.Sanitize(styled);

        sanitized.ShouldContain("style=");
        sanitized.ShouldContain("border-collapse: collapse");
    }

    [Fact]
    public void ApplyStyles_Typography_AddsInlineStyles()
    {
        var html = "<h1>Title</h1><h2>Subtitle</h2><h3>Section</h3><p>A <strong>bold</strong> paragraph.</p><ul><li>Item</li></ul>";

        var result = HtmlStyler.ApplyStyles(html);

        result.ShouldContain("<h1 style=\"font-size: 22px; font-weight: 600;\">");
        result.ShouldContain("<h2 style=\"font-size: 18px; font-weight: 600;\">");
        result.ShouldContain("<h3 style=\"font-size: 14px; font-weight: 600;\">");
        result.ShouldContain("<strong style=\"font-weight: 600;\">");
        result.ShouldContain("<p style=\"line-height: 1.6;\">");
        result.ShouldContain("<ul style=\"line-height: 1.8;\">");
    }

    [Fact]
    public void ApplyStyles_TodoList_AddsListStyleNone()
    {
        var html = """<ul class="contains-task-list"><li class="task-list-item">⭕ unchecked</li><li class="task-list-item">✅ checked</li></ul>""";

        var result = HtmlStyler.ApplyStyles(html);

        result.ShouldContain("list-style: none");
    }

    [Fact]
    public void ApplyStyles_RegularList_DoesNotAddListStyleNone()
    {
        var html = "<ul><li>Item</li></ul>";

        var result = HtmlStyler.ApplyStyles(html);

        result.ShouldNotContain("list-style: none");
    }

    [Fact]
    public void ApplyStyles_OrderedList_AddsLineHeight()
    {
        var html = "<ol><li>First</li><li>Second</li></ol>";

        var result = HtmlStyler.ApplyStyles(html);

        result.ShouldContain("<ol style=\"line-height: 1.8;\">");
    }
}
