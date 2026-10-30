using Microsoft.Extensions.Logging;
using Quill.Core.Models;

namespace Quill.Core.Markdown;

public static class WorkItemDescription
{
    public static async Task<(string Html, List<int> LinkIds)> ToHtmlAsync(
        string markdown, QuillConfig config, IAzureDevOpsClient client, ILogger? logger = null)
    {
        var (rawHtml, linkIds) = await MarkdownConverter.ToHtmlAsync(
            markdown, config.ServerUrl, config.Collection, config.Project, client, logger);

        var styledHtml = HtmlStyler.ApplyStyles(rawHtml);
        var sanitizer = new Ganss.Xss.HtmlSanitizer();
        sanitizer.AllowedAttributes.Add("style");
        sanitizer.AllowedAttributes.Add("data-vss-mention");
        sanitizer.AllowedAttributes.Add("data-lang");

        return (sanitizer.Sanitize(styledHtml), linkIds);
    }
}
