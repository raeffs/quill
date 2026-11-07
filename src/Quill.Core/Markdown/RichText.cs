using Microsoft.Extensions.Logging;
using Quill.Core.Models;

namespace Quill.Core.Markdown;

public sealed class RichText(QuillConfig config, IWorkItemBatchFetcher fetcher, ILogger<RichText> logger)
{
    private readonly Dictionary<int, WorkItem?> _workItems = [];

    public async Task<string> ToMarkdownAsync(string? html, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(html))
        {
            return string.Empty;
        }

        var markdown = await MarkdownConverter.ToMarkdownAsync(
            html, config.ServerUrl, config.Collection, config.Project, ids => LookUpAsync(ids, cancellationToken));
        return markdown.TrimEnd();
    }

    public async Task<(string Html, List<int> LinkIds)> ToHtmlAsync(string markdown, CancellationToken cancellationToken = default)
    {
        var (html, linkIds) = await MarkdownConverter.ToHtmlAsync(
            markdown, config.ServerUrl, config.Collection, config.Project, ids => LookUpAsync(ids, cancellationToken));
        return (StyleAndSanitize(html), linkIds);
    }

    internal static string StyleAndSanitize(string html)
    {
        var styledHtml = HtmlStyler.ApplyStyles(html);
        var sanitizer = new Ganss.Xss.HtmlSanitizer();
        sanitizer.AllowedAttributes.Add("style");
        sanitizer.AllowedAttributes.Add("data-vss-mention");
        sanitizer.AllowedAttributes.Add("data-lang");

        return sanitizer.Sanitize(styledHtml);
    }

    private async Task<IReadOnlyDictionary<int, WorkItem>> LookUpAsync(IReadOnlyCollection<int> ids, CancellationToken cancellationToken)
    {
        var newIds = ids.Where(id => !_workItems.ContainsKey(id)).ToList();
        if (newIds.Count > 0)
        {
            await FetchAsync(newIds, cancellationToken);
        }

        var found = new Dictionary<int, WorkItem>();
        foreach (var id in ids)
        {
            if (_workItems[id] is { } workItem)
            {
                found[id] = workItem;
            }
        }

        return found;
    }

    private async Task FetchAsync(List<int> ids, CancellationToken cancellationToken)
    {
        Exception? failure = null;
        try
        {
            var result = await fetcher.FetchAsync(ids, cancellationToken);
            foreach (var workItem in result.Items)
            {
                _workItems[workItem.Id] = workItem;
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException)
        {
            failure = ex;
        }

        foreach (var id in ids)
        {
            if (_workItems.TryAdd(id, null))
            {
                logger.LogWarning(failure, "Could not look up work item {WorkItemId} for a link; using the fallback link text", id);
            }
        }
    }
}
