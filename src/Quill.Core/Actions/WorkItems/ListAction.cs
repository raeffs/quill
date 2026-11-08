using Quill.Core.Models;

namespace Quill.Core.Actions.WorkItems;

public sealed record WiListRequest(
    string? Title,
    string? Assignee,
    IReadOnlyList<string> States,
    IReadOnlyList<string> Types,
    int Limit);

public static class ListAction
{
    public static async Task<IReadOnlyList<SearchResult>> RunAsync(
        IAzureDevOpsClient client,
        IWorkItemBatchFetcher fetcher,
        IIdentityClient identity,
        WiListRequest request,
        CancellationToken cancellationToken)
    {
        var assignee = request.Assignee;
        if (assignee is not null && !PersonResolver.IsMe(assignee))
        {
            var person = await new PersonResolver(identity).ResolveAsync("--assignee", assignee, cancellationToken);
            assignee = person.AssignedTo;
        }

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
