using System.Text.Json.Serialization;

namespace Quill.AzureDevOps.Dto;

internal sealed class PullRequestWorkItemRefsResponse
{
    [JsonPropertyName("value")]
    public IReadOnlyList<PullRequestWorkItemRef> Value { get; init; } = Array.Empty<PullRequestWorkItemRef>();
}
