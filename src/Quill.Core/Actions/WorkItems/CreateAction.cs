using Quill.Core.Markdown;
using Quill.Core.Models;
using Quill.Core.Validation;

namespace Quill.Core.Actions.WorkItems;

public static class CreateAction
{
    public static async Task<ViewResult> RunAsync(
        IAzureDevOpsClient client, RichText richText, WorkItemCreateRequest request)
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
            (var html, linkIds) = await richText.ToHtmlAsync(request.DescriptionMarkdown);
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
        var body = await richText.ToMarkdownAsync(created.Description);

        return ViewResultBuilder.Build(created, body, children: null);
    }
}
