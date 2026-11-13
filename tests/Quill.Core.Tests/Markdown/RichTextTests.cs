using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Quill.Core.Markdown;
using Quill.Core.Models;
using Quill.Tests.Shared;
using Shouldly;

namespace Quill.Core.Tests.Markdown;

public class RichTextTests
{
    private const string LinkTo123 = $"""<a href="{TestConstants.ServerUrl}/{TestConstants.Collection}/{TestConstants.Project}/_workitems/edit/123">the bug</a>""";
    private const string LinkTo456 = $"""<a href="{TestConstants.ServerUrl}/{TestConstants.Collection}/{TestConstants.Project}/_workitems/edit/456">the story</a>""";

    private static readonly QuillConfig Config = new()
    {
        ServerUrl = TestConstants.ServerUrl,
        Collection = TestConstants.Collection,
        Project = TestConstants.Project,
    };

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task ToMarkdownAsync_NullOrEmpty_ReturnsEmptyAndSendsNoFetch(string? html)
    {
        var fake = new FakeAzureDevOpsClient();

        var result = await CreateRichText(fake).ToMarkdownAsync(html, TestContext.Current.CancellationToken);

        result.ShouldBe(string.Empty);
        fake.Calls.ShouldBeEmpty();
    }

    [Fact]
    public async Task ToMarkdownAsync_TrimsTheEnd()
    {
        var result = await CreateRichText().ToMarkdownAsync("<p>Hello</p>", TestContext.Current.CancellationToken);

        result.ShouldBe("Hello");
    }

    [Fact]
    public async Task ToMarkdownAsync_NoLinks_SendsNoFetch()
    {
        var fake = new FakeAzureDevOpsClient();

        await CreateRichText(fake).ToMarkdownAsync("<p>plain</p>", TestContext.Current.CancellationToken);

        fake.Calls.ShouldBeEmpty();
    }

    [Fact]
    public async Task ToMarkdownAsync_FoundItem_NamesTypeAndTitle()
    {
        var fake = new FakeAzureDevOpsClient();
        fake.Add(LoginBug());

        var result = await CreateRichText(fake).ToMarkdownAsync($"<p>See {LinkTo123}.</p>", TestContext.Current.CancellationToken);

        result.ShouldContain("[Bug: Login fails](#123)");
    }

    [Fact]
    public async Task ToHtmlAsync_FoundItem_NamesTypeIdAndTitle()
    {
        var fake = new FakeAzureDevOpsClient();
        fake.Add(LoginBug());

        var (html, _) = await CreateRichText(fake).ToHtmlAsync("See [the bug](#123).", TestContext.Current.CancellationToken);

        html.ShouldContain(">Bug 123: Login fails</a>");
    }

    [Fact]
    public async Task ToMarkdownAsync_TwoLinksToOneItem_FetchesItOnce()
    {
        var fake = new FakeAzureDevOpsClient();
        fake.Add(LoginBug());

        await CreateRichText(fake).ToMarkdownAsync($"<p>See {LinkTo123} and {LinkTo123}.</p>", TestContext.Current.CancellationToken);

        fake.Calls.ShouldBe(["Fetch(123)"]);
    }

    [Fact]
    public async Task ToMarkdownAsync_SecondCall_UsesTheCache()
    {
        var fake = new FakeAzureDevOpsClient();
        fake.Add(LoginBug());
        var richText = CreateRichText(fake);

        await richText.ToMarkdownAsync($"<p>See {LinkTo123}.</p>", TestContext.Current.CancellationToken);
        await richText.ToMarkdownAsync($"<p>Again {LinkTo123}.</p>", TestContext.Current.CancellationToken);

        fake.Calls.ShouldBe(["Fetch(123)"]);
    }

    [Fact]
    public async Task ToMarkdownAsync_MissingItem_IsNotFetchedAgain()
    {
        var fake = new FakeAzureDevOpsClient();
        var richText = CreateRichText(fake);

        var first = await richText.ToMarkdownAsync($"<p>See {LinkTo123}.</p>", TestContext.Current.CancellationToken);
        var second = await richText.ToMarkdownAsync($"<p>Again {LinkTo123}.</p>", TestContext.Current.CancellationToken);

        first.ShouldContain("[#123](#123)");
        second.ShouldContain("[#123](#123)");
        fake.Calls.ShouldBe(["Fetch(123)"]);
    }

    [Fact]
    public async Task ToMarkdownAsync_SameMissingItemTwice_LogsOneWarning()
    {
        var logger = new CapturingLogger();
        var richText = new RichText(Config, new FakeAzureDevOpsClient(), logger);

        await richText.ToMarkdownAsync($"<p>See {LinkTo123}.</p>", TestContext.Current.CancellationToken);
        await richText.ToMarkdownAsync($"<p>Again {LinkTo123}.</p>", TestContext.Current.CancellationToken);

        var entry = logger.Entries.ShouldHaveSingleItem();
        entry.Level.ShouldBe(LogLevel.Warning);
        entry.Message.ShouldContain("123");
    }

    [Fact]
    public async Task ToMarkdownAsync_AfterFetchException_IsNotFetchedAgain()
    {
        var fake = new FakeAzureDevOpsClient { FetchException = new UnavailableException() };
        var richText = CreateRichText(fake);

        await richText.ToMarkdownAsync($"<p>See {LinkTo123}.</p>", TestContext.Current.CancellationToken);
        var second = await richText.ToMarkdownAsync($"<p>Again {LinkTo123}.</p>", TestContext.Current.CancellationToken);

        fake.Calls.ShouldBe(["Fetch(123)"]);
        second.ShouldContain("[#123](#123)");
    }

    [Fact]
    public async Task ToHtmlAsync_ThenToMarkdownAsync_SharesTheCache()
    {
        var fake = new FakeAzureDevOpsClient();
        fake.Add(LoginBug());
        var richText = CreateRichText(fake);

        var (html, _) = await richText.ToHtmlAsync("See [the bug](#123).", TestContext.Current.CancellationToken);
        await richText.ToMarkdownAsync(html, TestContext.Current.CancellationToken);

        fake.Calls.ShouldBe(["Fetch(123)"]);
    }

    [Fact]
    public async Task ToMarkdownAsync_BatchFailedId_FallsBack()
    {
        var fake = new FakeAzureDevOpsClient();
        fake.BatchFailedIds.Add(123);

        var result = await CreateRichText(fake).ToMarkdownAsync($"<p>See {LinkTo123}.</p>", TestContext.Current.CancellationToken);

        result.ShouldContain("[#123](#123)");
    }

    [Theory]
    [InlineData(typeof(UnavailableException))]
    [InlineData(typeof(DeniedException))]
    [InlineData(typeof(RejectedException))]
    public async Task FetchThrows_FallsBackForEveryLink(Type exceptionType)
    {
        var fake = new FakeAzureDevOpsClient();
        fake.Add(LoginBug());
        fake.FetchException = (Exception)Activator.CreateInstance(exceptionType)!;

        var markdown = await CreateRichText(fake).ToMarkdownAsync($"<p>See {LinkTo123} and {LinkTo456}.</p>", TestContext.Current.CancellationToken);
        var (html, linkIds) = await CreateRichText(fake).ToHtmlAsync("See [the bug](#123).", TestContext.Current.CancellationToken);

        markdown.ShouldContain("[#123](#123)");
        markdown.ShouldContain("[#456](#456)");
        html.ShouldContain(">the bug</a>");
        linkIds.ShouldBe([123]);
    }

    [Fact]
    public async Task ToMarkdownAsync_LookupDefect_IsNotSwallowed()
    {
        var fake = new FakeAzureDevOpsClient { FetchException = new DefectException("Failed to deserialize work items batch response.") };
        var richText = CreateRichText(fake);

        await Should.ThrowAsync<DefectException>(() => richText.ToMarkdownAsync($"<p>See {LinkTo123}.</p>", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task FetchCancelled_Throws()
    {
        var fake = new FakeAzureDevOpsClient { FetchException = new OperationCanceledException() };
        var richText = CreateRichText(fake);

        await Should.ThrowAsync<OperationCanceledException>(() => richText.ToMarkdownAsync($"<p>See {LinkTo123}.</p>", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ToHtmlAsync_SanitisesAndStyles()
    {
        var (html, _) = await CreateRichText().ToHtmlAsync("<script>x</script>\n\n**b**", TestContext.Current.CancellationToken);

        html.ShouldNotContain("<script");
        html.ShouldContain("""<strong style="font-weight: 600">b</strong>""");
    }

    [Fact]
    public async Task ToHtmlAsync_BasicMarkdown_ReturnsHtml()
    {
        var markdown = "## Hello\n\nThis is a **bold** paragraph.";

        var (html, linkIds) = await CreateRichText().ToHtmlAsync(markdown, TestContext.Current.CancellationToken);

        html.ShouldContain("""<h2 style="font-size: 18px; font-weight: 600">Hello</h2>""");
        html.ShouldContain("""<strong style="font-weight: 600">bold</strong>""");
        linkIds.ShouldBeEmpty();
    }

    [Fact]
    public async Task ToHtmlAsync_WithWorkItemLink_RewritesLinkAndCollectsId()
    {
        var markdown = "See [auth work](#456) for details.";

        var (html, linkIds) = await CreateRichText().ToHtmlAsync(markdown, TestContext.Current.CancellationToken);

        html.ShouldContain(
            """data-vss-mention="version:1.0" href="https://myserver.com/tfs/DefaultCollection/MyProject/_workitems/edit/456""");
        html.ShouldContain(">auth work</a>");
        linkIds.ShouldBe([456], ignoreOrder: true);
    }

    [Fact]
    public async Task ToHtmlAsync_MultipleWorkItemLinks_CollectsAllIds()
    {
        var markdown = "See [item A](#100) and [item B](#200).";

        var (html, linkIds) = await CreateRichText().ToHtmlAsync(markdown, TestContext.Current.CancellationToken);

        linkIds.ShouldBe([100, 200], ignoreOrder: true);
    }

    [Fact]
    public async Task ToHtmlAsync_TodoList_ConvertsCheckboxesToEmojis()
    {
        var markdown = "- [ ] unchecked item\n- [x] checked item\n- [ ] another unchecked";

        var (html, _) = await CreateRichText().ToHtmlAsync(markdown, TestContext.Current.CancellationToken);

        html.ShouldContain("⭕ unchecked item");
        html.ShouldContain("✅ checked item");
        html.ShouldContain("⭕ another unchecked");
        html.ShouldNotContain("<input");
    }

    [Fact]
    public async Task ToHtmlAsync_TodoList_ProducesUnorderedList()
    {
        var markdown = "- [ ] item one\n- [x] item two";

        var (html, _) = await CreateRichText().ToHtmlAsync(markdown, TestContext.Current.CancellationToken);

        html.ShouldContain("<ul");
        html.ShouldContain("<li");
    }

    [Fact]
    public async Task ToHtmlAsync_MixedList_OnlyConvertsCheckboxItems()
    {
        var markdown = "- [ ] todo item\n- regular item";

        var (html, _) = await CreateRichText().ToHtmlAsync(markdown, TestContext.Current.CancellationToken);

        html.ShouldContain("⭕ todo item");
        html.ShouldContain("regular item");
        html.ShouldNotContain("⭕ regular item");
        html.ShouldNotContain("✅ regular item");
    }

    [Fact]
    public async Task ToMarkdownAsync_TodoListWithEmojis_ConvertsToCheckboxSyntax()
    {
        var html = "<ul><li>⭕ unchecked item</li><li>✅ checked item</li><li>⭕ another unchecked</li></ul>";

        var markdown = await CreateRichText().ToMarkdownAsync(html, TestContext.Current.CancellationToken);

        markdown.ShouldContain("- [ ] unchecked item");
        markdown.ShouldContain("- [x] checked item");
        markdown.ShouldContain("- [ ] another unchecked");
    }

    [Fact]
    public async Task ToMarkdownAsync_MixedListWithEmojis_OnlyConvertsEmojiItems()
    {
        var html = "<ul><li>⭕ todo item</li><li>regular item</li></ul>";

        var markdown = await CreateRichText().ToMarkdownAsync(html, TestContext.Current.CancellationToken);

        markdown.ShouldContain("- [ ] todo item");
        markdown.ShouldContain("- regular item");
    }

    [Fact]
    public async Task ToMarkdownAsync_BasicHtml_ReturnsMarkdown()
    {
        var html = "<h2>Hello</h2><p>This is a <strong>bold</strong> paragraph.</p>";

        var markdown = await CreateRichText().ToMarkdownAsync(html, TestContext.Current.CancellationToken);

        markdown.ShouldContain("## Hello");
        markdown.ShouldContain("**bold**");
    }

    [Fact]
    public async Task ToHtmlAsync_PreservesCurlyBracesInText()
    {
        var markdown = "- **Path:** /files/{id}";

        var (html, _) = await CreateRichText().ToHtmlAsync(markdown, TestContext.Current.CancellationToken);

        html.ShouldContain("{id}");
    }

    [Fact]
    public async Task ToHtmlAsync_WithNestedBracketsInLinkText_RewritesLinkAndCollectsId()
    {
        var markdown = "- Depends on: [Product Backlog Item: [ABC-1] GET /items/{id} - Get Item](#1234)";

        var (html, linkIds) = await CreateRichText().ToHtmlAsync(markdown, TestContext.Current.CancellationToken);

        html.ShouldContain(
            """href="https://myserver.com/tfs/DefaultCollection/MyProject/_workitems/edit/1234""");
        linkIds.ShouldBe([1234], ignoreOrder: true);
    }

    [Fact]
    public async Task ToMarkdownAsync_WithNestedBracketsInLinkText_RewritesToHashLink()
    {
        var html = """<p><a href="https://myserver.com/tfs/DefaultCollection/MyProject/_workitems/edit/1234">Product Backlog Item: [ABC-1] GET /items/{id} - Get Item</a></p>""";

        var markdown = await CreateRichText().ToMarkdownAsync(html, TestContext.Current.CancellationToken);

        markdown.ShouldContain("[#1234](#1234)");
    }

    [Fact]
    public async Task ToMarkdownAsync_WithWorkItemLink_RewritesToHashLink()
    {
        var html = """<p>See <a href="https://myserver.com/tfs/DefaultCollection/MyProject/_workitems/edit/456">auth work</a> for details.</p>""";

        var markdown = await CreateRichText().ToMarkdownAsync(html, TestContext.Current.CancellationToken);

        markdown.ShouldContain("[#456](#456)");
    }

    [Fact]
    public async Task ToMarkdownAsync_PreservesSpaceAfterInlineCodeBeforeNestedList()
    {
        var html = "<ul><li>call <code>POST /folders</code>, and optionally <code>parentFolderId</code> if a folder is selected.\n<ul><li>On success: close.</li></ul></li></ul>";

        var markdown = await CreateRichText().ToMarkdownAsync(html, TestContext.Current.CancellationToken);

        markdown.ShouldContain("`parentFolderId` if a folder");
    }

    [Fact]
    public async Task ToMarkdownAsync_KeepsNestedListsTight()
    {
        var html = "<ul><li>call <code>POST /folders</code>, and optionally <code>parentFolderId</code> if a folder is selected.\n<ul><li>On success: close.</li></ul></li></ul>";

        var markdown = await CreateRichText().ToMarkdownAsync(html, TestContext.Current.CancellationToken);

        markdown.ShouldNotContain("is selected.\n\n");
    }

    [Fact]
    public async Task ToMarkdownAsync_DecodesHtmlEntitiesInInlineContent()
    {
        var html = "<p>Folders can be nested (e.g., <code>Inbox</code> &gt; <code>Archive</code>).</p>";

        var markdown = await CreateRichText().ToMarkdownAsync(html, TestContext.Current.CancellationToken);

        markdown.ShouldContain("`Inbox` > `Archive`");
        markdown.ShouldNotContain("&gt;");
    }

    [Fact]
    public async Task RoundTrip_SpaceAfterBoldDroppedByAzureDevOps()
    {
        var originalMarkdown = "- **`New File` button** - opens the Upload File modal";

        var (html, _) = await CreateRichText().ToHtmlAsync(originalMarkdown, TestContext.Current.CancellationToken);
        var azDoHtml = html.Replace("</strong> - ", "</strong>- ", StringComparison.Ordinal);

        var result = await CreateRichText().ToMarkdownAsync(azDoHtml, TestContext.Current.CancellationToken);

        // The space is lost because AzDO stripped it from the HTML — not a bug in our converter
        result.ShouldContain("**`New File` button**- opens the Upload File modal");
    }

    [Fact]
    public async Task ToMarkdownAsync_WithGuidProjectUrl_RewritesToHashLink()
    {
        var html = """<p><a href="https://myserver.com/tfs/DefaultCollection/6c1f3d2e-0000-4000-8000-000000000001/_workitems/edit/456">Product Backlog Item 456: Some Title</a></p>""";

        var markdown = await CreateRichText().ToMarkdownAsync(html, TestContext.Current.CancellationToken);

        markdown.ShouldContain("[#456](#456)");
    }

    [Theory]
    [InlineData("https://tfs.contoso.com:8080/tfs/DefaultCollection/MyProject/_workitems/edit/1002")]
    [InlineData("https://devops.contoso.com/DefaultCollection/MyProject/_workitems/edit/1002")]
    [InlineData("http://tfs.contoso.com/DefaultCollection/MyProject/_workitems/edit/1002")]
    [InlineData("https://tfs.contoso.com:8080/tfs/DefaultCollection/6c1f3d2e-0000-4000-8000-000000000001/_workitems/edit/1002")]
    public async Task ToMarkdownAsync_WithWorkItemLinkOnAnotherHost_RewritesToHashLink(string url)
    {
        var html = $"""<p><a href="{url}">Product Backlog Item 1002: Retire the legacy resource group</a></p>""";

        var markdown = await CreateRichText().ToMarkdownAsync(html, TestContext.Current.CancellationToken);

        markdown.ShouldBe("[#1002](#1002)");
    }

    [Fact]
    public async Task ToMarkdownAsync_WithTwoForeignHostLinksOnOneLine_RewritesBoth()
    {
        var html = """<p><a href="https://tfs.contoso.com:8080/tfs/DefaultCollection/MyProject/_workitems/edit/1002">one</a> and <a href="https://tfs.contoso.com:8080/tfs/DefaultCollection/MyProject/_workitems/edit/1003">two</a></p>""";

        var markdown = await CreateRichText().ToMarkdownAsync(html, TestContext.Current.CancellationToken);

        markdown.ShouldBe("[#1002](#1002) and [#1003](#1003)");
    }

    [Theory]
    [InlineData("https://tfs.contoso.com:8080/tfs/OtherCollection/MyProject/_workitems/edit/1002")]
    [InlineData("https://tfs.contoso.com:8080/tfs/MyDefaultCollection/MyProject/_workitems/edit/1002")]
    [InlineData("https://tfs.contoso.com:8080/tfs/defaultcollection/MyProject/_workitems/edit/1002")]
    public async Task ToMarkdownAsync_WithWorkItemLinkInAnotherCollection_KeepsFullUrl(string url)
    {
        var html = $"""<p><a href="{url}">Product Backlog Item 1002</a></p>""";

        var markdown = await CreateRichText().ToMarkdownAsync(html, TestContext.Current.CancellationToken);

        markdown.ShouldBe($"[Product Backlog Item 1002]({url})");
    }

    [Fact]
    public async Task ToHtmlAsync_EmptyMarkdown_ReturnsEmptyString()
    {
        var (html, linkIds) = await CreateRichText().ToHtmlAsync(string.Empty, TestContext.Current.CancellationToken);

        html.ShouldBeEmpty();
        linkIds.ShouldBeEmpty();
    }

    [Fact]
    public async Task ToMarkdownAsync_EmptyHtml_ReturnsEmptyString()
    {
        var markdown = await CreateRichText().ToMarkdownAsync(string.Empty, TestContext.Current.CancellationToken);

        markdown.Trim().ShouldBeEmpty();
    }

    [Fact]
    public async Task ToHtmlAsync_NoWorkItemLinks_ReturnsHtmlWithEmptyLinkIds()
    {
        var markdown = "Just a paragraph with **bold** and *italic*.";

        var (html, linkIds) = await CreateRichText().ToHtmlAsync(markdown, TestContext.Current.CancellationToken);

        html.ShouldContain("""<strong style="font-weight: 600">bold</strong>""");
        html.ShouldContain("<em>italic</em>");
        linkIds.ShouldBeEmpty();
    }

    [Fact]
    public async Task ToMarkdownAsync_NoWorkItemLinks_ReturnsMarkdownUnchanged()
    {
        var html = "<p>Simple paragraph with <strong>bold</strong>.</p>";

        var markdown = await CreateRichText().ToMarkdownAsync(html, TestContext.Current.CancellationToken);

        markdown.ShouldContain("**bold**");
        markdown.ShouldNotContain("href");
    }

    [Fact]
    public async Task RoundTrip_TodoList_PreservesCheckboxSyntax()
    {
        var originalMarkdown = "- [ ] unchecked item\n- [x] checked item\n- [ ] another unchecked";

        var (html, _) = await CreateRichText().ToHtmlAsync(originalMarkdown, TestContext.Current.CancellationToken);
        var styledHtml = HtmlStyler.ApplyStyles(html);
        var result = await CreateRichText().ToMarkdownAsync(styledHtml, TestContext.Current.CancellationToken);

        result.ShouldContain("- [ ] unchecked item");
        result.ShouldContain("- [x] checked item");
        result.ShouldContain("- [ ] another unchecked");
    }

    [Fact]
    public async Task ToHtmlAsync_FencedCodeBlockWithCSharp_ProducesSyntaxHighlightedDiv()
    {
        var markdown = "```csharp\npublic class Foo { }\n```";

        var (html, _) = await CreateRichText().ToHtmlAsync(markdown, TestContext.Current.CancellationToken);

        html.ShouldContain("data-lang=\"csharp\"");
        html.ShouldContain("background-color: rgba(48, 52, 70, 1)");
        html.ShouldNotContain("<pre");
    }

    [Fact]
    public async Task ToHtmlAsync_FencedCodeBlockWithSql_ProducesSyntaxHighlightedDiv()
    {
        var markdown = "```sql\nSELECT * FROM users\n```";

        var (html, _) = await CreateRichText().ToHtmlAsync(markdown, TestContext.Current.CancellationToken);

        html.ShouldContain("data-lang=\"sql\"");
        html.ShouldNotContain("<pre");
    }

    [Fact]
    public async Task ToHtmlAsync_FencedCodeBlockWithoutLanguage_KeepsPreCode()
    {
        var markdown = "```\nplain code\n```";

        var (html, _) = await CreateRichText().ToHtmlAsync(markdown, TestContext.Current.CancellationToken);

        html.ShouldContain("<pre style=");
        html.ShouldContain("<code>");
        html.ShouldNotContain("data-lang");
    }

    [Fact]
    public async Task ToHtmlAsync_FencedCodeBlockWithUnsupportedLanguage_KeepsPreCode()
    {
        var markdown = "```ruby\nputs 'hello'\n```";

        var (html, _) = await CreateRichText().ToHtmlAsync(markdown, TestContext.Current.CancellationToken);

        html.ShouldContain("<pre style=");
        html.ShouldContain("<code");
        html.ShouldNotContain("data-lang");
    }

    [Fact]
    public async Task ToMarkdownAsync_StyledHtml_ProducesCleanMarkdown()
    {
        var markdown = "## Heading\n\n| H1 | H2 |\n|---|---|\n| A | B |\n\n> A quote\n\n`inline code`\n\n```\ncode block\n```\n";

        var (rawHtml, _) = await CreateRichText().ToHtmlAsync(markdown, TestContext.Current.CancellationToken);
        var styledHtml = HtmlStyler.ApplyStyles(rawHtml);

        var result = await CreateRichText().ToMarkdownAsync(styledHtml, TestContext.Current.CancellationToken);

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

        var markdown = await CreateRichText().ToMarkdownAsync(html, TestContext.Current.CancellationToken);

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

        var markdown = await CreateRichText().ToMarkdownAsync(html, TestContext.Current.CancellationToken);

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

        var markdown = await CreateRichText().ToMarkdownAsync(html, TestContext.Current.CancellationToken);

        markdown.ShouldContain("```");
        markdown.ShouldContain("var x = 1;");
        markdown.ShouldNotContain("```csharp");
        markdown.ShouldNotContain("```javascript");
    }

    [Fact]
    public async Task RoundTrip_SyntaxHighlightedCodeBlock_PreservesContent()
    {
        var originalMarkdown = "```csharp\npublic class Foo { }\n```";

        var (html, _) = await CreateRichText().ToHtmlAsync(originalMarkdown, TestContext.Current.CancellationToken);
        var markdown = await CreateRichText().ToMarkdownAsync(html, TestContext.Current.CancellationToken);

        markdown.ShouldContain("```csharp");
        markdown.ShouldContain("public class Foo { }");
    }

    [Fact]
    public async Task RoundTrip_MixedContentWithCodeBlocks_PreservesAll()
    {
        var markdown = "## Title\n\nSome text.\n\n```csharp\npublic class Foo\n{\n    var x = \"hello\";\n}\n```\n\nMore text.\n\n```\nplain code\n```\n";

        var (html, _) = await CreateRichText().ToHtmlAsync(markdown, TestContext.Current.CancellationToken);
        var styledHtml = HtmlStyler.ApplyStyles(html);
        var result = await CreateRichText().ToMarkdownAsync(styledHtml, TestContext.Current.CancellationToken);

        result.ShouldContain("## Title");
        result.ShouldContain("Some text.");
        result.ShouldContain("```csharp");
        result.ShouldContain("public class Foo");
        result.ShouldContain("More text.");
        result.ShouldNotContain("style=");
        result.ShouldNotContain("data-lang");
    }

    private static RichText CreateRichText(FakeAzureDevOpsClient? fake = null) =>
        new(Config, fake ?? new FakeAzureDevOpsClient(), NullLogger<RichText>.Instance);

    private static WorkItem LoginBug() => new()
    {
        Id = 123,
        Type = "Bug",
        Title = "Login fails",
        State = "New",
        AssignedToId = string.Empty,
    };

    private sealed class CapturingLogger : ILogger<RichText>
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            Entries.Add((logLevel, formatter(state, exception)));
    }
}
