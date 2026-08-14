using System.Text.Json.Serialization;
using Quill.AzureDevOps.Dto;

namespace Quill.AzureDevOps;

[JsonSerializable(typeof(List<JsonPatchOperation>))]
[JsonSerializable(typeof(WorkItemResponse))]
[JsonSerializable(typeof(RelationValue))]
[JsonSerializable(typeof(WorkItemsBatchRequest))]
[JsonSerializable(typeof(WorkItemsBatchResponse))]
[JsonSerializable(typeof(WiqlQueryRequest))]
[JsonSerializable(typeof(WiqlQueryResponse))]
[JsonSerializable(typeof(CommentsResponse))]
[JsonSerializable(typeof(PullRequestListResponse))]
[JsonSerializable(typeof(PullRequestItemResponse))]
[JsonSerializable(typeof(PullRequestThreadsResponse))]
[JsonSerializable(typeof(PullRequestWorkItemRefsResponse))]
[JsonSerializable(typeof(PullRequestIterationsResponse))]
[JsonSerializable(typeof(PullRequestIterationChangesResponse))]
[JsonSerializable(typeof(PullRequestFileDiffsRequest))]
[JsonSerializable(typeof(List<PullRequestFileDiffEntry>))]
internal sealed partial class AzureDevOpsJsonContext : JsonSerializerContext
{
}
