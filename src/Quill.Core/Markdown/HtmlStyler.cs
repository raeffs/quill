using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html;

namespace Quill.Core.Markdown;

public static class HtmlStyler
{
    private const string TableStyle = "border-collapse: collapse;";
    private const string ThStyle = "background-color: #f0f0f0; border: 1px solid #ddd; padding: 8px 16px; text-align: left; font-weight: 600;";
    private const string TdStyle = "border: 1px solid #ddd; padding: 8px 16px;";
    private const string H1Style = "font-size: 22px; font-weight: 600;";
    private const string H2Style = "font-size: 18px; font-weight: 600;";
    private const string HStyle = "font-size: 14px; font-weight: 600;";
    private const string StrongStyle = "font-weight: 600;";
    private const string UlStyle = "line-height: 1.8;";
    private const string TodoUlStyle = "line-height: 1.8; list-style: none; padding-left: 0;";
    private const string OlStyle = "line-height: 1.8;";
    private const string PStyle = "line-height: 1.6;";
    private const string PreStyle = "background-color: #f5f5f5; border: 1px solid #ddd; border-radius: 4px; padding: 12px; font-family: monospace; overflow-x: auto;";
    private const string InlineCodeStyle = "background-color: #f5f5f5; padding: 2px 6px; border-radius: 3px; font-family: monospace;";
    private const string BlockquoteStyle = "border-left: 4px solid #0078d4; padding: 8px 16px; margin: 8px 0; background-color: #f8f8f8;";

    public static string ApplyStyles(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return html;
        }

        return ApplyStylesAsync(html).GetAwaiter().GetResult();
    }

    private static async Task<string> ApplyStylesAsync(string html)
    {
        var context = BrowsingContext.New(Configuration.Default);
        var document = await context.OpenAsync(req => req.Content(html)).ConfigureAwait(false);

        ApplyTableStyles(document);
        ApplyCodeStyles(document);
        ApplyBlockquoteStyles(document);
        ApplyTypographyStyles(document);

        // Serialize just the body content (AngleSharp wraps fragments in a full document)
        using var writer = new StringWriter();
        await document.Body!.ToHtmlAsync(writer).ConfigureAwait(false);
        var bodyHtml = writer.ToString();

        const string bodyOpen = "<body>";
        const string bodyClose = "</body>";
        var start = bodyHtml.IndexOf(bodyOpen, StringComparison.OrdinalIgnoreCase);
        var end = bodyHtml.LastIndexOf(bodyClose, StringComparison.OrdinalIgnoreCase);
        return start >= 0 && end > start
            ? bodyHtml[(start + bodyOpen.Length)..end]
            : bodyHtml;
    }

    private static void ApplyTableStyles(IDocument document)
    {
        foreach (var table in document.QuerySelectorAll("table"))
        {
            SetStyle(table, TableStyle);
        }

        foreach (var th in document.QuerySelectorAll("th"))
        {
            SetStyle(th, ThStyle);
        }

        foreach (var td in document.QuerySelectorAll("td"))
        {
            SetStyle(td, TdStyle);
        }
    }

    private static void ApplyCodeStyles(IDocument document)
    {
        foreach (var pre in document.QuerySelectorAll("pre"))
        {
            SetStyle(pre, PreStyle);
        }

        foreach (var code in document.QuerySelectorAll("code"))
        {
            if (code.ParentElement?.TagName.Equals("PRE", StringComparison.OrdinalIgnoreCase) != true)
            {
                SetStyle(code, InlineCodeStyle);
            }
        }
    }

    private static void ApplyBlockquoteStyles(IDocument document)
    {
        foreach (var blockquote in document.QuerySelectorAll("blockquote"))
        {
            SetStyle(blockquote, BlockquoteStyle);
        }
    }

    private static void ApplyTypographyStyles(IDocument document)
    {
        foreach (var h1 in document.QuerySelectorAll("h1"))
        {
            SetStyle(h1, H1Style);
        }

        foreach (var h2 in document.QuerySelectorAll("h2"))
        {
            SetStyle(h2, H2Style);
        }

        foreach (var h in document.QuerySelectorAll("h3, h4, h5, h6"))
        {
            SetStyle(h, HStyle);
        }

        foreach (var strong in document.QuerySelectorAll("strong"))
        {
            SetStyle(strong, StrongStyle);
        }

        foreach (var ul in document.QuerySelectorAll("ul"))
        {
            var isTodoList = ul.ClassList.Contains("contains-task-list")
                || ul.TextContent.Contains('⭕', StringComparison.Ordinal)
                || ul.TextContent.Contains('✅', StringComparison.Ordinal);
            SetStyle(ul, isTodoList ? TodoUlStyle : UlStyle);
        }

        foreach (var ol in document.QuerySelectorAll("ol"))
        {
            SetStyle(ol, OlStyle);
        }

        foreach (var p in document.QuerySelectorAll("p"))
        {
            SetStyle(p, PStyle);
        }
    }

    private static void SetStyle(IElement element, string style)
    {
        element.SetAttribute("style", style);
    }
}
