using Microsoft.Extensions.Logging;
using Quill.Core.Markdown;
using Quill.Core.Models;
using Quill.Core.Validation;

namespace Quill.Core.Actions;

public static class WiCreateAction
{
    public static async Task<ViewResult> RunAsync(
        IAzureDevOpsClient client, QuillConfig config, ILogger logger, WorkItemCreateRequest request)
    {
        var parent = await client.GetWorkItemAsync(request.ParentId);

        var validation = ParentValidator.ValidateType(request.Type, parent);
        if (!validation.IsValid)
        {
            throw new InvalidRequestException(string.Join("; ", validation.Errors));
        }

        string? descriptionHtml = null;
        List<int> linkIds = [];
        if (!string.IsNullOrEmpty(request.DescriptionMarkdown))
        {
            (var html, linkIds) = await WorkItemDescription.ToHtmlAsync(request.DescriptionMarkdown, config, client, logger);
            descriptionHtml = string.IsNullOrEmpty(html) ? null : html;
        }

        var newId = await client.CreateWorkItemAsync(new WorkItemCreatePatch(
            request.Type,
            request.Title,
            request.ParentId,
            AssignedToId: request.Assignee,
            DescriptionHtml: descriptionHtml,
            IterationPath: parent.IterationPath,
            AreaPath: parent.AreaPath,
            Tags: request.Tags,
            Predecessors: request.Predecessors));

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

        return ViewResultBuilder.Build(created, body, children: null);
    }
}
