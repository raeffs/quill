using Microsoft.Extensions.Logging;
using Quill.Core.Markdown;
using Quill.Core.Models;
using Quill.Core.Validation;

namespace Quill.Core.Actions;

public sealed record BacklogCreateOutcome(CreateResult Row, string FileContent);

public static class BacklogCreateAction
{
    public static async Task<BacklogCreateOutcome> RunAsync(
        IAzureDevOpsClient client,
        IIdentityClient identity,
        QuillConfig config,
        ILogger logger,
        string fileContent,
        string? assignedTo)
    {
        var parsed = FrontmatterParser.Parse(fileContent);

        if (parsed.Id != 0)
        {
            throw new InvalidRequestException("Work item id must be 0 for creation.");
        }

        if (string.IsNullOrWhiteSpace(parsed.Title))
        {
            throw new InvalidRequestException("Frontmatter must contain a non-empty 'title' field.");
        }

        if (string.IsNullOrWhiteSpace(parsed.Type))
        {
            throw new InvalidRequestException("Frontmatter must contain a 'type' field.");
        }

        if (!WorkItemHierarchy.ChildTypes.Contains(parsed.Type, StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidRequestException($"Type must be one of: {string.Join(", ", WorkItemHierarchy.ChildTypes)}.");
        }

        if (parsed.ParentId is null or 0)
        {
            throw new InvalidRequestException("Frontmatter must contain a non-zero 'parentId' field.");
        }

        var parentWorkItem = await client.GetWorkItemAsync(parsed.ParentId.Value);
        var currentUser = await identity.GetCurrentUserAsync();

        var validation = ParentValidator.Validate(parsed.Type, parentWorkItem, currentUser.Id);
        if (!validation.IsValid)
        {
            throw new InvalidRequestException(string.Join("; ", validation.Errors));
        }

        var assignee = string.IsNullOrWhiteSpace(assignedTo) ? currentUser.DisplayName : assignedTo;

        var (descriptionHtml, linkIds) = await WorkItemDescription.ToHtmlAsync(parsed.Body, config, client, logger);

        var newId = await client.CreateWorkItemAsync(new WorkItemCreatePatch(
            parsed.Type,
            parsed.Title,
            parsed.ParentId.Value,
            AssignedToId: assignee,
            DescriptionHtml: string.IsNullOrEmpty(descriptionHtml) ? null : descriptionHtml,
            IterationPath: parentWorkItem.IterationPath,
            AreaPath: parentWorkItem.AreaPath,
            Tags: parsed.Tags,
            Predecessors: parsed.Predecessors));

        foreach (var targetId in linkIds)
        {
            await client.AddRelationAsync(newId, targetId);
        }

        var updatedContent = FrontmatterParser.Write(
            id: newId,
            type: parsed.Type,
            title: parsed.Title,
            state: "New",
            body: parsed.Body.TrimEnd(),
            parentId: parsed.ParentId,
            tags: parsed.Tags ?? [],
            predecessors: parsed.Predecessors ?? []);

        return new BacklogCreateOutcome(new CreateResult { Id = newId, Title = parsed.Title }, updatedContent);
    }
}
