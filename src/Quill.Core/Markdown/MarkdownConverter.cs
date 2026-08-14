using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using AngleSharp;
using AngleSharp.Dom;
using Markdig;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ReverseMarkdown;

namespace Quill.Core.Markdown;

public static partial class MarkdownConverter
{
    public static async Task<(string Html, List<int> WorkItemLinkIds)> ToHtmlAsync(
        string markdown,
        string serverUrl,
        string collection,
        string project,
        IAzureDevOpsClient? client = null,
        ILogger? logger = null)
    {
        logger ??= NullLogger.Instance;
        var linkIds = new List<int>();
        var baseUrl = $"{serverUrl.TrimEnd('/')}/{collection}/{project}/_workitems/edit";

        foreach (Match match in WorkItemLinkRegex().Matches(markdown))
        {
            linkIds.Add(int.Parse(match.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture));
        }

        var workItemTexts = new Dictionary<int, string>();
        if (client is not null)
        {
            foreach (var id in linkIds)
            {
                try
                {
                    var wi = await client.GetWorkItemAsync(id);
                    workItemTexts[id] = $"{wi.Type} {wi.Id}: {wi.Title}";
                }
                catch (HttpRequestException ex)
                {
                    logger.LogWarning(ex, "Failed to fetch work item {WorkItemId} for link resolution; keeping original link text", id);
                }
            }
        }

        var rewritten = WorkItemLinkRegex().Replace(markdown, match =>
        {
            var originalText = match.Groups[1].Value;
            var id = int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
            var linkText = workItemTexts.GetValueOrDefault(id, originalText);
            return $"[{linkText}]({baseUrl}/{id})";
        });

        // UseAdvancedExtensions includes UseAutoIdentifiers which adds id attributes to headings.
        // We build the pipeline manually to include useful extensions without auto-identifiers.
        var pipeline = new MarkdownPipelineBuilder()
            .UseEmphasisExtras()
            .UsePipeTables()
            .UseGridTables()
            .UseFootnotes()
            .UseCitations()
            .UseCustomContainers()
            .UseFigures()
            .UseFooters()
            .UseMathematics()

            .UseDefinitionLists()
            .UseTaskLists()
            .UseSmartyPants()
            .UseAbbreviations()
            .Build();
        var html = Markdig.Markdown.ToHtml(rewritten, pipeline);
        html = await ApplySyntaxHighlightingAsync(html);

        // Replace task list checkboxes with emojis (Azure DevOps doesn't support <input> well)
        html = html.Replace("""<input disabled="disabled" type="checkbox" checked="checked" />""", "✅", StringComparison.Ordinal);
        html = html.Replace("""<input disabled="disabled" type="checkbox" />""", "⭕", StringComparison.Ordinal);

        // Add data-vss-mention attribute to work item links so Azure DevOps renders them
        // as rich mention widgets (with icon, state badge, etc.) instead of plain links.
        var linkPrefix = $@"<a href=""{baseUrl}/";
        html = html.Replace(linkPrefix, $@"<a data-vss-mention=""version:1.0"" href=""{baseUrl}/", StringComparison.Ordinal);

        return (html, linkIds);
    }

    public static async Task<string> ToMarkdownAsync(
        string html,
        string serverUrl,
        string collection,
        string project,
        IAzureDevOpsClient? client = null,
        ILogger? logger = null)
    {
        logger ??= NullLogger.Instance;
        html = await ConvertCodeDivsToPreCodeAsync(html);
        html = await PreserveTrailingTextSpaceBeforeNestedBlockAsync(html);
        var converter = new ReverseMarkdown.Converter(new ReverseMarkdown.Config { GithubFlavored = true });
        var markdown = converter.Convert(html);

        // ReverseMarkdown may leave HTML entities (e.g. &gt;, &amp;) un-decoded in the output
        markdown = WebUtility.HtmlDecode(markdown);

        // Restore the non-breaking spaces inserted by PreserveTrailingTextSpaceBeforeNestedBlockAsync
        markdown = markdown.Replace('\u00A0', ' ');

        // Remove trailing whitespace from each line (ReverseMarkdown adds trailing spaces)
        markdown = TrailingWhitespaceRegex().Replace(markdown, string.Empty);

        markdown = TodoUncheckedRegex().Replace(markdown, "- [ ] ");
        markdown = TodoCheckedRegex().Replace(markdown, "- [x] ");

        // The [^/]+ segment matches both project-name and GUID-based URLs.
        var serverUrlEscaped = Regex.Escape($"{serverUrl.TrimEnd('/')}/{collection}");
        var pattern = $@"\[((?:[^\[\]]|\[[^\]]*\])+)\]\({serverUrlEscaped}/[^/]+/_workitems/edit/(\d+)\)";
        var regex = new Regex(pattern, RegexOptions.None, TimeSpan.FromSeconds(5));

        var workItemTexts = new Dictionary<int, string>();
        if (client is not null)
        {
            foreach (Match match in regex.Matches(markdown))
            {
                var id = int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
                if (!workItemTexts.ContainsKey(id))
                {
                    try
                    {
                        var wi = await client.GetWorkItemAsync(id);
                        workItemTexts[id] = $"{wi.Type}: {wi.Title}";
                    }
                    catch (HttpRequestException ex)
                    {
                        logger.LogWarning(ex, "Failed to fetch work item {WorkItemId} for link resolution; falling back to #{WorkItemId}", id, id);
                    }
                }
            }
        }

        markdown = regex.Replace(markdown, match =>
        {
            var id = int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
            var linkText = workItemTexts.GetValueOrDefault(id, $"#{id}");
            return $"[{linkText}](#{id})";
        });

        return markdown;
    }

    private static async Task<string> ConvertCodeDivsToPreCodeAsync(string html)
    {
        var context = AngleSharp.BrowsingContext.New(AngleSharp.Configuration.Default);
        var document = await context.OpenAsync(req => req.Content(html));

        var modified = false;

        foreach (var div in document.QuerySelectorAll("div[data-lang]").ToList())
        {
            var language = div.GetAttribute("data-lang") ?? string.Empty;
            var text = ExtractTextFromCodeDiv(div);
            var pre = document.CreateElement("pre");
            var code = document.CreateElement("code");
            if (!string.IsNullOrEmpty(language))
            {
                code.ClassList.Add($"language-{language}");
            }

            code.TextContent = text;
            pre.AppendChild(code);
            div.ReplaceWith(pre);
            modified = true;
        }

        // Also detect VS Code-pasted code (div with monospace font-family, no data-lang)
        foreach (var div in document.QuerySelectorAll("div[style]").ToList())
        {
            var style = div.GetAttribute("style") ?? string.Empty;
            if (!style.Contains("monospace", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // Must have nested divs with spans to look like a code block
            if (div.QuerySelector("div > span[style], div > span") is null)
            {
                continue;
            }

            if (div.HasAttribute("data-lang"))
            {
                continue;
            }

            var text = ExtractTextFromCodeDiv(div);
            var pre = document.CreateElement("pre");
            var code = document.CreateElement("code");
            code.TextContent = text;
            pre.AppendChild(code);
            div.ReplaceWith(pre);
            modified = true;
        }

        if (!modified)
        {
            return html;
        }

        using var writer = new System.IO.StringWriter();
        await document.Body!.ToHtmlAsync(writer);
        var bodyHtml = writer.ToString();
        const string bodyOpen = "<body>";
        const string bodyClose = "</body>";
        var start = bodyHtml.IndexOf(bodyOpen, StringComparison.OrdinalIgnoreCase);
        var end = bodyHtml.LastIndexOf(bodyClose, StringComparison.OrdinalIgnoreCase);
        return start >= 0 && end > start
            ? bodyHtml[(start + bodyOpen.Length)..end]
            : bodyHtml;
    }

    // ReverseMarkdown strips leading whitespace from the last text node before a block child of
    // an <li> (e.g. `</code> if` → `` `X`if ``). Replace that leading whitespace with a non-breaking
    // space so ReverseMarkdown preserves it; it's converted back to a regular space after conversion.
    private static async Task<string> PreserveTrailingTextSpaceBeforeNestedBlockAsync(string html)
    {
        var context = AngleSharp.BrowsingContext.New(AngleSharp.Configuration.Default);
        var document = await context.OpenAsync(req => req.Content(html));

        var modified = false;

        foreach (var li in document.QuerySelectorAll("li").ToList())
        {
            var firstBlockChild = li.Children.FirstOrDefault(IsBlockElement);
            if (firstBlockChild is null)
            {
                continue;
            }

            INode? lastTextNode = null;
            foreach (var node in li.ChildNodes)
            {
                if (node == firstBlockChild)
                {
                    break;
                }

                if (node.NodeType == NodeType.Text && !string.IsNullOrWhiteSpace(node.TextContent))
                {
                    lastTextNode = node;
                }
            }

            if (lastTextNode is null)
            {
                continue;
            }

            var text = lastTextNode.TextContent;
            var leadingSpaces = 0;
            while (leadingSpaces < text.Length && text[leadingSpaces] == ' ')
            {
                leadingSpaces++;
            }

            if (leadingSpaces == 0)
            {
                continue;
            }

            lastTextNode.TextContent = new string('\u00A0', leadingSpaces) + text[leadingSpaces..];
            modified = true;
        }

        if (!modified)
        {
            return html;
        }

        using var writer = new System.IO.StringWriter();
        await document.Body!.ToHtmlAsync(writer);
        var bodyHtml = writer.ToString();
        const string bodyOpen = "<body>";
        const string bodyClose = "</body>";
        var start = bodyHtml.IndexOf(bodyOpen, StringComparison.OrdinalIgnoreCase);
        var end = bodyHtml.LastIndexOf(bodyClose, StringComparison.OrdinalIgnoreCase);
        return start >= 0 && end > start
            ? bodyHtml[(start + bodyOpen.Length)..end]
            : bodyHtml;
    }

    private static bool IsBlockElement(IElement element) => element.TagName switch
    {
        "UL" or "OL" or "P" or "DIV" or "TABLE" or "PRE" or "BLOCKQUOTE"
            or "H1" or "H2" or "H3" or "H4" or "H5" or "H6"
            or "HR" or "DL" or "FIGURE" or "DETAILS" or "SECTION" => true,
        _ => false,
    };

    private static string ExtractTextFromCodeDiv(AngleSharp.Dom.IElement div)
    {
        var lines = new List<string>();
        foreach (var child in div.Children)
        {
            if (child.TagName.Equals("DIV", StringComparison.OrdinalIgnoreCase))
            {
                // Each inner div is one line of the code block.
                lines.Add(child.TextContent.Replace('\u00A0', ' '));
            }
            else if (child.TagName.Equals("BR", StringComparison.OrdinalIgnoreCase))
            {
                lines.Add(string.Empty);
            }
        }

        return string.Join('\n', lines);
    }

    private static async Task<string> ApplySyntaxHighlightingAsync(string html)
    {
        var context = BrowsingContext.New(Configuration.Default);
        var document = await context.OpenAsync(req => req.Content(html));

        var modified = false;
        foreach (var pre in document.QuerySelectorAll("pre").ToList())
        {
            var code = pre.QuerySelector("code");
            if (code is null)
            {
                continue;
            }

            var langClass = code.ClassList.FirstOrDefault(c => c.StartsWith("language-", StringComparison.Ordinal));
            if (langClass is null)
            {
                continue;
            }

            var language = langClass["language-".Length..];
            var sourceCode = code.TextContent;
            var highlighted = SyntaxHighlighter.Highlight(sourceCode, language);
            if (highlighted is null)
            {
                continue;
            }

            pre.OuterHtml = highlighted;
            modified = true;
        }

        if (!modified)
        {
            return html;
        }

        using var writer = new System.IO.StringWriter();
        await document.Body!.ToHtmlAsync(writer);
        var bodyHtml = writer.ToString();
        const string bodyOpen = "<body>";
        const string bodyClose = "</body>";
        var start = bodyHtml.IndexOf(bodyOpen, StringComparison.OrdinalIgnoreCase);
        var end = bodyHtml.LastIndexOf(bodyClose, StringComparison.OrdinalIgnoreCase);
        return start >= 0 && end > start
            ? bodyHtml[(start + bodyOpen.Length)..end]
            : bodyHtml;
    }

    [GeneratedRegex(@"\[((?:[^\[\]]|\[[^\]]*\])+)\]\(#(\d+)\)", RegexOptions.NonBacktracking)]
    private static partial Regex WorkItemLinkRegex();

    [GeneratedRegex(@"[^\S\n]+$", RegexOptions.Multiline | RegexOptions.NonBacktracking)]
    private static partial Regex TrailingWhitespaceRegex();

    [GeneratedRegex(@"^- ⭕\s*", RegexOptions.Multiline | RegexOptions.NonBacktracking)]
    private static partial Regex TodoUncheckedRegex();

    [GeneratedRegex(@"^- ✅\s*", RegexOptions.Multiline | RegexOptions.NonBacktracking)]
    private static partial Regex TodoCheckedRegex();
}
