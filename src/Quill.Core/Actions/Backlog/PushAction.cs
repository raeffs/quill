using Quill.Core.Markdown;
using Quill.Core.Models;
using Quill.Core.Validation;

namespace Quill.Core.Actions.Backlog;

public static class PushAction
{
    public static async Task<PushResult> RunAsync(
        IAzureDevOpsClient client,
        IIdentityClient identity,
        QuillConfig config,
        RichText richText,
        string fileContent)
    {
        var parsed = FrontmatterParser.Parse(fileContent);

        if (parsed.Id < 1)
        {
            throw new UsageException("Work item id must be a positive integer for a push. To create the work item, set id to 0 and run 'backlog create'.");
        }

        var workItem = await client.GetWorkItemAsync(parsed.Id);
        var currentUser = await identity.GetCurrentUserAsync();

        var validation = PushValidator.Validate(workItem, config, currentUser.Id);
        if (!validation.IsValid)
        {
            throw new RejectedException(string.Join("; ", validation.Errors));
        }

        var (html, linkIds) = await richText.ToHtmlAsync(parsed.Body);

        var predecessorChange = WorkItemPredecessors.Diff(workItem.Predecessors, parsed.Predecessors);

        await client.UpdateWorkItemAsync(new WorkItemUpdatePatch(parsed.Id, workItem.Type, parsed.Title, html)
        {
            Tags = parsed.Tags,
            Rev = workItem.Rev,
            RemoveRelationIndexes = predecessorChange.RemoveRelationIndexes,
            AddRelations = [.. predecessorChange.Add.Select(id => new WorkItemRelation { RelationType = WorkItemLinkTypes.Predecessor, TargetId = id })],
        });

        var existingRelatedIds = workItem.Relations
            .Where(r => string.Equals(r.RelationType, WorkItemLinkTypes.Related, StringComparison.Ordinal))
            .Select(r => r.TargetId)
            .ToHashSet();

        var newRelations = linkIds.Where(id => !existingRelatedIds.Contains(id)).ToList();
        foreach (var targetId in newRelations)
        {
            await client.AddRelationAsync(parsed.Id, targetId);
        }

        return new PushResult
        {
            Id = parsed.Id,
            Title = parsed.Title,
            UpdatedFields = UpdatedFieldsFor(parsed, predecessorChange.HasChanges),
            RelationsAdded = newRelations,
        };
    }

    private static List<string> UpdatedFieldsFor(ParsedMarkdownFile parsed, bool predecessorsChanged)
    {
        var fields = new List<string> { "title", "description" };

        if (parsed.Tags is not null)
        {
            fields.Add("tags");
        }

        if (predecessorsChanged)
        {
            fields.Add("predecessors");
        }

        return fields;
    }
}
