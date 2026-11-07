using Quill.Core.Models;

namespace Quill.Core.Actions;

public sealed record WiListRequest(
    string? Title,
    string? Assignee,
    IReadOnlyList<string> States,
    IReadOnlyList<string> Types,
    int Limit);

public static class WiListAction
{
    public static async Task<IReadOnlyList<SearchResult>> RunAsync(
        IAzureDevOpsClient client,
        IWorkItemBatchFetcher fetcher,
        WiListRequest request,
        CancellationToken cancellationToken)
    {
        var (wiql, top) = WiqlBuilder.Build(request.Title, request.Assignee, request.States, request.Types, request.Limit);

        var ids = await client.QueryByWiqlAsync(wiql, top, cancellationToken);
        var batch = await fetcher.FetchAsync(ids, cancellationToken);

        var byId = batch.Items.ToDictionary(i => i.Id);
        return ids
            .Where(byId.ContainsKey)
            .Select(id => SearchResultBuilder.Build(byId[id]))
            .ToList();
    }
}
