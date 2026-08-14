using Quill.Core.Markdown;
using Quill.Tests.Shared;
using Shouldly;

namespace Quill.Core.Tests.Markdown;

public class MarkdownConverterTests
{
    [Fact]
    public async Task ToHtmlAsync_BasicMarkdown_ReturnsHtml()
    {
        var markdown = "## Hello\n\nThis is a **bold** paragraph.";

        var (html, linkIds) = await MarkdownConverter.ToHtmlAsync(markdown, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        html.ShouldContain("<h2>Hello</h2>");
        html.ShouldContain("<strong>bold</strong>");
        linkIds.ShouldBeEmpty();
    }

    [Fact]
    public async Task ToHtmlAsync_WithWorkItemLink_RewritesLinkAndCollectsId()
    {
        var markdown = "See [auth work](#456) for details.";

        var (html, linkIds) = await MarkdownConverter.ToHtmlAsync(markdown, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        html.ShouldContain(
            """data-vss-mention="version:1.0" href="https://myserver.com/tfs/DefaultCollection/MyProject/_workitems/edit/456""");

        // Without a client, original text is preserved
        html.ShouldContain(">auth work</a>");
        linkIds.ShouldBe([456], ignoreOrder: true);
    }

    [Fact]
    public async Task ToHtmlAsync_MultipleWorkItemLinks_CollectsAllIds()
    {
        var markdown = "See [item A](#100) and [item B](#200).";

        var (html, linkIds) = await MarkdownConverter.ToHtmlAsync(markdown, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        linkIds.ShouldBe([100, 200], ignoreOrder: true);
    }

    [Fact]
    public async Task ToHtmlAsync_TodoList_ConvertsCheckboxesToEmojis()
    {
        var markdown = "- [ ] unchecked item\n- [x] checked item\n- [ ] another unchecked";

        var (html, _) = await MarkdownConverter.ToHtmlAsync(markdown, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        html.ShouldContain("⭕ unchecked item");
        html.ShouldContain("✅ checked item");
        html.ShouldContain("⭕ another unchecked");
        html.ShouldNotContain("<input");
    }

    [Fact]
    public async Task ToHtmlAsync_TodoList_ProducesUnorderedList()
    {
        var markdown = "- [ ] item one\n- [x] item two";

        var (html, _) = await MarkdownConverter.ToHtmlAsync(markdown, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        html.ShouldContain("<ul");
        html.ShouldContain("<li");
    }

    [Fact]
    public async Task ToHtmlAsync_MixedList_OnlyConvertsCheckboxItems()
    {
        var markdown = "- [ ] todo item\n- regular item";

        var (html, _) = await MarkdownConverter.ToHtmlAsync(markdown, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        html.ShouldContain("⭕ todo item");
        html.ShouldContain("regular item");
        html.ShouldNotContain("⭕ regular item");
        html.ShouldNotContain("✅ regular item");
    }

    [Fact]
    public async Task ToMarkdownAsync_TodoListWithEmojis_ConvertsToCheckboxSyntax()
    {
        var html = "<ul><li>⭕ unchecked item</li><li>✅ checked item</li><li>⭕ another unchecked</li></ul>";

        var markdown = await MarkdownConverter.ToMarkdownAsync(html, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        markdown.ShouldContain("- [ ] unchecked item");
        markdown.ShouldContain("- [x] checked item");
        markdown.ShouldContain("- [ ] another unchecked");
    }

    [Fact]
    public async Task ToMarkdownAsync_MixedListWithEmojis_OnlyConvertsEmojiItems()
    {
        var html = "<ul><li>⭕ todo item</li><li>regular item</li></ul>";

        var markdown = await MarkdownConverter.ToMarkdownAsync(html, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        markdown.ShouldContain("- [ ] todo item");
        markdown.ShouldContain("- regular item");
    }

    [Fact]
    public async Task ToMarkdownAsync_BasicHtml_ReturnsMarkdown()
    {
        var html = "<h2>Hello</h2><p>This is a <strong>bold</strong> paragraph.</p>";

        var markdown = await MarkdownConverter.ToMarkdownAsync(html, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        markdown.ShouldContain("## Hello");
        markdown.ShouldContain("**bold**");
    }

    [Fact]
    public async Task ToHtmlAsync_PreservesCurlyBracesInText()
    {
        var markdown = "- **Path:** /files/{id}";

        var (html, _) = await MarkdownConverter.ToHtmlAsync(markdown, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        html.ShouldContain("{id}");
    }

    [Fact]
    public async Task ToHtmlAsync_WithNestedBracketsInLinkText_RewritesLinkAndCollectsId()
    {
        var markdown = "- Depends on: [Product Backlog Item: [API-13] GET /files/{id}/versions - List File Versions](#58069)";

        var (html, linkIds) = await MarkdownConverter.ToHtmlAsync(markdown, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        html.ShouldContain(
            """href="https://myserver.com/tfs/DefaultCollection/MyProject/_workitems/edit/58069""");
        linkIds.ShouldBe([58069], ignoreOrder: true);
    }

    [Fact]
    public async Task ToMarkdownAsync_WithNestedBracketsInLinkText_RewritesToHashLink()
    {
        var html = """<p><a href="https://myserver.com/tfs/DefaultCollection/MyProject/_workitems/edit/58069">Product Backlog Item: [API-13] GET /files/{id}/versions - List File Versions</a></p>""";

        var markdown = await MarkdownConverter.ToMarkdownAsync(html, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        markdown.ShouldContain("[#58069](#58069)");
    }

    [Fact]
    public async Task ToMarkdownAsync_WithWorkItemLink_RewritesToHashLink()
    {
        var html = """<p>See <a href="https://myserver.com/tfs/DefaultCollection/MyProject/_workitems/edit/456">auth work</a> for details.</p>""";

        var markdown = await MarkdownConverter.ToMarkdownAsync(html, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        // Without a client, falls back to #{id}
        markdown.ShouldContain("[#456](#456)");
    }

    [Fact]
    public async Task ToMarkdownAsync_PreservesSpaceAfterInlineCodeBeforeNestedList()
    {
        var html = "<ul><li>call <code>POST /folders</code>, and optionally <code>parentFolderId</code> if a folder is selected.\n<ul><li>On success: close.</li></ul></li></ul>";

        var markdown = await MarkdownConverter.ToMarkdownAsync(html, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        markdown.ShouldContain("`parentFolderId` if a folder");
    }

    [Fact]
    public async Task ToMarkdownAsync_KeepsNestedListsTight()
    {
        var html = "<ul><li>call <code>POST /folders</code>, and optionally <code>parentFolderId</code> if a folder is selected.\n<ul><li>On success: close.</li></ul></li></ul>";

        var markdown = await MarkdownConverter.ToMarkdownAsync(html, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        markdown.ShouldNotContain("is selected.\n\n");
    }

    [Fact]
    public async Task ToMarkdownAsync_DecodesHtmlEntitiesInInlineContent()
    {
        var html = "<p>Folders can be nested (e.g., <code>Zuzug</code> &gt; <code>aus den USA</code>).</p>";

        var markdown = await MarkdownConverter.ToMarkdownAsync(html, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        markdown.ShouldContain("`Zuzug` > `aus den USA`");
        markdown.ShouldNotContain("&gt;");
    }

    [Fact]
    public async Task RoundTrip_SpaceAfterBoldDroppedByAzureDevOps()
    {
        var originalMarkdown = "- **`Neue Datei` button** - opens the Upload File modal";

        var (html, _) = await MarkdownConverter.ToHtmlAsync(originalMarkdown, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);
        var azDoHtml = html.Replace("</strong> - ", "</strong>- ", StringComparison.Ordinal);

        var result = await MarkdownConverter.ToMarkdownAsync(azDoHtml, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        // The space is lost because AzDO stripped it from the HTML — not a bug in our converter
        result.ShouldContain("**`Neue Datei` button**- opens the Upload File modal");
    }

    [Fact]
    public async Task ToMarkdownAsync_WithGuidProjectUrl_RewritesToHashLink()
    {
        var html = """<p><a href="https://myserver.com/tfs/DefaultCollection/05c52ca9-c1e3-485f-bda7-f8efa1689e87/_workitems/edit/456">Product Backlog Item 456: Some Title</a></p>""";

        var markdown = await MarkdownConverter.ToMarkdownAsync(html, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        // Without a client, falls back to #{id}
        markdown.ShouldContain("[#456](#456)");
    }

    [Fact]
    public async Task ToHtmlAsync_EmptyMarkdown_ReturnsEmptyString()
    {
        var (html, linkIds) = await MarkdownConverter.ToHtmlAsync(
            string.Empty, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        html.ShouldBeEmpty();
        linkIds.ShouldBeEmpty();
    }

    [Fact]
    public async Task ToMarkdownAsync_EmptyHtml_ReturnsEmptyString()
    {
        var markdown = await MarkdownConverter.ToMarkdownAsync(
            string.Empty, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        markdown.Trim().ShouldBeEmpty();
    }

    [Fact]
    public async Task ToHtmlAsync_NoWorkItemLinks_ReturnsHtmlWithEmptyLinkIds()
    {
        var markdown = "Just a paragraph with **bold** and *italic*.";

        var (html, linkIds) = await MarkdownConverter.ToHtmlAsync(
            markdown, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        html.ShouldContain("<strong>bold</strong>");
        html.ShouldContain("<em>italic</em>");
        linkIds.ShouldBeEmpty();
    }

    [Fact]
    public async Task ToMarkdownAsync_NoWorkItemLinks_ReturnsMarkdownUnchanged()
    {
        var html = "<p>Simple paragraph with <strong>bold</strong>.</p>";

        var markdown = await MarkdownConverter.ToMarkdownAsync(
            html, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        markdown.ShouldContain("**bold**");
        markdown.ShouldNotContain("href");
    }

    [Fact]
    public async Task RoundTrip_TodoList_PreservesCheckboxSyntax()
    {
        var originalMarkdown = "- [ ] unchecked item\n- [x] checked item\n- [ ] another unchecked";

        var (html, _) = await MarkdownConverter.ToHtmlAsync(originalMarkdown, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);
        var styledHtml = HtmlStyler.ApplyStyles(html);
        var result = await MarkdownConverter.ToMarkdownAsync(styledHtml, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        result.ShouldContain("- [ ] unchecked item");
        result.ShouldContain("- [x] checked item");
        result.ShouldContain("- [ ] another unchecked");
    }

    [Fact]
    public async Task ToHtmlAsync_FencedCodeBlockWithCSharp_ProducesSyntaxHighlightedDiv()
    {
        var markdown = "```csharp\npublic class Foo { }\n```";

        var (html, _) = await MarkdownConverter.ToHtmlAsync(markdown, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        html.ShouldContain("data-lang=\"csharp\"");
        html.ShouldContain("background-color: #303446;");
        html.ShouldNotContain("<pre>");
    }

    [Fact]
    public async Task ToHtmlAsync_FencedCodeBlockWithSql_ProducesSyntaxHighlightedDiv()
    {
        var markdown = "```sql\nSELECT * FROM users\n```";

        var (html, _) = await MarkdownConverter.ToHtmlAsync(markdown, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        html.ShouldContain("data-lang=\"sql\"");
        html.ShouldNotContain("<pre>");
    }

    [Fact]
    public async Task ToHtmlAsync_FencedCodeBlockWithoutLanguage_KeepsPreCode()
    {
        var markdown = "```\nplain code\n```";

        var (html, _) = await MarkdownConverter.ToHtmlAsync(markdown, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        html.ShouldContain("<pre>");
        html.ShouldContain("<code>");
        html.ShouldNotContain("data-lang");
    }

    [Fact]
    public async Task ToHtmlAsync_FencedCodeBlockWithUnsupportedLanguage_KeepsPreCode()
    {
        var markdown = "```ruby\nputs 'hello'\n```";

        var (html, _) = await MarkdownConverter.ToHtmlAsync(markdown, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        html.ShouldContain("<pre>");
        html.ShouldContain("<code");
        html.ShouldNotContain("data-lang");
    }

    [Fact]
    public async Task ToMarkdownAsync_StyledHtml_ProducesCleanMarkdown()
    {
        var markdown = "## Heading\n\n| H1 | H2 |\n|---|---|\n| A | B |\n\n> A quote\n\n`inline code`\n\n```\ncode block\n```\n";

        var (rawHtml, _) = await MarkdownConverter.ToHtmlAsync(markdown, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);
        var styledHtml = HtmlStyler.ApplyStyles(rawHtml);

        var result = await MarkdownConverter.ToMarkdownAsync(styledHtml, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        result.ShouldContain("## Heading");
        result.ShouldContain("|");
        result.ShouldNotContain("style=");
        result.ShouldNotContain("border-collapse");
    }

    [Fact]
    public async Task ToMarkdownAsync_SyntaxHighlightedDiv_ProducesFencedCodeBlock()
    {
        var html = """
            <div data-lang="csharp" style="color: #c6d0f5; background-color: #303446; font-family: Consolas, monospace;">
                <div><span style="color: #ca9ee6;">public</span>&nbsp;<span style="color: #ca9ee6;">class</span>&nbsp;<span style="color: #e5c890;">Foo</span>&nbsp;{&nbsp;}</div>
            </div>
            """;

        var markdown = await MarkdownConverter.ToMarkdownAsync(html, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        markdown.ShouldContain("```csharp");
        markdown.ShouldContain("public class Foo { }");
        markdown.ShouldContain("```");
    }

    [Fact]
    public async Task ToMarkdownAsync_SyntaxHighlightedDivMultiLine_PreservesLines()
    {
        var html = """
            <div data-lang="csharp" style="color: #c6d0f5; background-color: #303446;">
                <div><span style="color: #ca9ee6;">public</span>&nbsp;<span style="color: #ca9ee6;">class</span>&nbsp;<span style="color: #e5c890;">Foo</span></div>
                <div><span>{</span></div>
                <div><span>}</span></div>
            </div>
            """;

        var markdown = await MarkdownConverter.ToMarkdownAsync(html, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        markdown.ShouldContain("```csharp");
        markdown.ShouldContain("public class Foo");
        markdown.ShouldContain("{");
        markdown.ShouldContain("}");
    }

    [Fact]
    public async Task ToMarkdownAsync_VsCodePastedCode_ProducesPlainFencedCodeBlock()
    {
        // VS Code paste format: div with monospace font-family but no data-lang
        var html = """
            <div style="color: #d4d4d4; background-color: #1e1e1e; font-family: Consolas, 'Courier New', monospace;">
                <div><span style="color: #569cd6;">var</span>&nbsp;<span>x</span>&nbsp;=&nbsp;<span style="color: #b5cea8;">1</span>;</div>
            </div>
            """;

        var markdown = await MarkdownConverter.ToMarkdownAsync(html, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        markdown.ShouldContain("```");
        markdown.ShouldContain("var x = 1;");
        markdown.ShouldNotContain("```csharp");
        markdown.ShouldNotContain("```javascript");
    }

    [Fact]
    public async Task RoundTrip_SyntaxHighlightedCodeBlock_PreservesContent()
    {
        var originalMarkdown = "```csharp\npublic class Foo { }\n```";

        var (html, _) = await MarkdownConverter.ToHtmlAsync(originalMarkdown, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);
        var markdown = await MarkdownConverter.ToMarkdownAsync(html, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        markdown.ShouldContain("```csharp");
        markdown.ShouldContain("public class Foo { }");
    }

    [Fact]
    public async Task RoundTrip_MixedContentWithCodeBlocks_PreservesAll()
    {
        var markdown = "## Title\n\nSome text.\n\n```csharp\npublic class Foo\n{\n    var x = \"hello\";\n}\n```\n\nMore text.\n\n```\nplain code\n```\n";

        var (html, _) = await MarkdownConverter.ToHtmlAsync(markdown, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);
        var styledHtml = HtmlStyler.ApplyStyles(html);
        var result = await MarkdownConverter.ToMarkdownAsync(styledHtml, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        result.ShouldContain("## Title");
        result.ShouldContain("Some text.");
        result.ShouldContain("```csharp");
        result.ShouldContain("public class Foo");
        result.ShouldContain("More text.");
        result.ShouldNotContain("style=");
        result.ShouldNotContain("data-lang");
    }
}
