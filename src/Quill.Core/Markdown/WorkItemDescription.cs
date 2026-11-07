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

        return (RichText.StyleAndSanitize(rawHtml), linkIds);
    }
}
