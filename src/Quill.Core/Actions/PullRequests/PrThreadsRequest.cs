namespace Quill.Core.Actions.PullRequests;

public sealed record PrThreadsRequest(int Id, IReadOnlyList<string> Statuses, int? Limit);
