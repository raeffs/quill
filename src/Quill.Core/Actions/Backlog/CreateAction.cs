using Quill.Core.Markdown;
using Quill.Core.Models;
using Quill.Core.Validation;

namespace Quill.Core.Actions.Backlog;

public sealed record BacklogCreateOutcome(CreateResult Row, string FileContent);

public static class CreateAction
{
    public static async Task<BacklogCreateOutcome> RunAsync(
        IAzureDevOpsClient client,
        IIdentityClient identity,
        RichText richText,
        string fileContent,
        string? assignee)
    {
        var parsed = FrontmatterParser.Parse(fileContent);

        if (parsed.Id != 0)
        {
            throw new UsageException("Work item id must be 0 for creation.");
        }

        if (string.IsNullOrWhiteSpace(parsed.Title))
        {
            throw new UsageException("Frontmatter must contain a non-empty 'title' field.");
        }

        if (string.IsNullOrWhiteSpace(parsed.Type))
        {
            throw new UsageException("Frontmatter must contain a 'type' field.");
        }

        if (!WorkItemHierarchy.ChildTypes.Contains(parsed.Type, StringComparer.OrdinalIgnoreCase))
        {
            throw new UsageException($"Type must be one of: {string.Join(", ", WorkItemHierarchy.ChildTypes)}.");
        }

        if (parsed.ParentId is null or < 1)
        {
            throw new UsageException("Frontmatter must contain a positive 'parentId' field.");
        }

        var parentWorkItem = await client.GetWorkItemAsync(parsed.ParentId.Value);
        var resolver = new PersonResolver(identity);
        var currentUser = await resolver.MeAsync();

        var validation = ParentValidator.Validate(parsed.Type, parentWorkItem, currentUser.Id);
        if (!validation.IsValid)
        {
            throw new RejectedException(string.Join("; ", validation.Errors));
        }

        var person = await resolver.ResolveAsync("--assignee", assignee ?? "@me", CancellationToken.None);

        var (descriptionHtml, linkIds) = await richText.ToHtmlAsync(parsed.Body);

        var newId = await client.CreateWorkItemAsync(new WorkItemCreatePatch(
            parsed.Type,
            parsed.Title,
            parsed.ParentId.Value,
            AssignedToId: person.AssignedTo,
            DescriptionHtml: string.IsNullOrEmpty(descriptionHtml) ? null : descriptionHtml,
            IterationPath: parentWorkItem.IterationPath,
            AreaPath: parentWorkItem.AreaPath,
            Tags: parsed.Tags,
            Predecessors: parsed.Predecessors));

        await CreatedWorkItem.LinkAsync(client, newId, linkIds);

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
