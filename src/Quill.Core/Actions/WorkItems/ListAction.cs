using Quill.Core.Models;

namespace Quill.Core.Actions.WorkItems;

public static class ListAction
{
    public static async Task<IReadOnlyList<SearchResult>> RunAsync(
        IAzureDevOpsClient client,
        IWorkItemBatchFetcher fetcher,
        PersonResolver people,
        WiListRequest request,
        CancellationToken cancellationToken)
    {
        var assignee = request.Assignee is null
            ? null
            : await people.ResolveForQueryAsync("--assignee", request.Assignee, cancellationToken);

        var (wiql, top) = WiqlBuilder.Build(request.Title, assignee, request.States, request.Types, request.Limit);

        var ids = await client.QueryByWiqlAsync(wiql, top, cancellationToken);
        var batch = await fetcher.FetchAsync(ids, cancellationToken);

        var byId = batch.Items.ToDictionary(i => i.Id);
        return ids
            .Where(byId.ContainsKey)
            .Select(id => SearchResultBuilder.Build(byId[id]))
            .ToList();
    }
}
