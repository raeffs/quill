using Quill.Core.Markdown;
using Quill.Core.Models;
using Quill.Core.Validation;

namespace Quill.Core.Actions.WorkItems;

public static class CreateAction
{
    public static async Task<ViewResult> RunAsync(
        IAzureDevOpsClient client, IIdentityClient identity, RichText richText, WorkItemCreateRequest request)
    {
        var parent = await client.GetWorkItemAsync(request.ParentId);

        var validation = ParentValidator.ValidateType(request.Type, parent);
        if (!validation.IsValid)
        {
            throw new RejectedException(string.Join("; ", validation.Errors));
        }

        string? assignedTo = null;
        if (request.Assignee is not null)
        {
            var person = await new PersonResolver(identity).ResolveAsync("--assignee", request.Assignee, CancellationToken.None);
            assignedTo = person.AssignedTo;
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
            AssignedToId: assignedTo,
            DescriptionHtml: descriptionHtml,
            IterationPath: parent.IterationPath,
            AreaPath: parent.AreaPath,
            Tags: request.Tags,
            Predecessors: request.Predecessors));

        await CreatedWorkItem.LinkAsync(client, newId, linkIds);

        // Read the item back so the row is what `wi view` would print: server state, rewritten link text.
        var created = await ReadBackAsync(client, newId);
        var body = await richText.ToMarkdownAsync(created.Description);

        return ViewResultBuilder.Build(created, body, children: null);
    }

    private static async Task<WorkItem> ReadBackAsync(IAzureDevOpsClient client, int id)
    {
        try
        {
            return await client.GetWorkItemAsync(id);
        }
        catch (UnavailableException exception)
        {
            throw new UnavailableException(CreatedWorkItem.Failed(id, "reading it failed", exception), exception.RetryAfter, exception);
        }
        catch (RejectedException exception)
        {
            throw new RejectedException(CreatedWorkItem.Failed(id, "reading it failed", exception), exception);
        }
        catch (DeniedException exception)
        {
            throw new DeniedException(CreatedWorkItem.Failed(id, "reading it failed", exception), exception);
        }
        catch (UsageException exception)
        {
            throw new UsageException(CreatedWorkItem.Failed(id, "reading it failed", exception), exception);
        }
    }
}
