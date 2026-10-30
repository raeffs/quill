using Microsoft.Extensions.Logging;
using Quill.Core.Markdown;
using Quill.Core.Models;
using Quill.Core.Validation;

namespace Quill.Core;

public static class WorkItemCreator
{
    public static async Task<WorkItemCreateOutcome> CreateAsync(
        IAzureDevOpsClient client, QuillConfig config, WorkItemCreateRequest request, ILogger? logger = null)
    {
        var parent = await client.GetWorkItemAsync(request.ParentId);

        var validation = ParentValidator.ValidateType(request.Type, parent);
        if (!validation.IsValid)
        {
            return new WorkItemCreateOutcome(validation, null);
        }

        string? descriptionHtml = null;
        List<int> linkIds = [];
        if (!string.IsNullOrEmpty(request.DescriptionMarkdown))
        {
            (var html, linkIds) = await WorkItemDescription.ToHtmlAsync(request.DescriptionMarkdown, config, client, logger);
            descriptionHtml = string.IsNullOrEmpty(html) ? null : html;
        }

        var newId = await client.CreateWorkItemAsync(
            request.Type, request.Title, request.ParentId, request.Assignee, descriptionHtml, parent.IterationPath);

        foreach (var targetId in linkIds)
        {
            await client.AddRelationAsync(newId, targetId);
        }

        // Read the item back so the row is what `wi view` would print: server state, rewritten link text.
        var created = await client.GetWorkItemAsync(newId);
        var body = string.IsNullOrEmpty(created.Description)
            ? string.Empty
            : (await MarkdownConverter.ToMarkdownAsync(
                created.Description, config.ServerUrl, config.Collection, config.Project, client, logger)).TrimEnd();

        return new WorkItemCreateOutcome(validation, ViewResultBuilder.Build(created, body, children: null));
    }
}
