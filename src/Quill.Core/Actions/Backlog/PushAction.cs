using Microsoft.Extensions.Logging;
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
        ILogger logger,
        string fileContent)
    {
        var parsed = FrontmatterParser.Parse(fileContent);

        var workItem = await client.GetWorkItemAsync(parsed.Id);
        var currentUser = await identity.GetCurrentUserAsync();

        var validation = PushValidator.Validate(workItem, config, currentUser.Id);
        if (!validation.IsValid)
        {
            throw new InvalidRequestException(string.Join("; ", validation.Errors));
        }

        var (html, linkIds) = await WorkItemDescription.ToHtmlAsync(parsed.Body, config, client, logger);

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
