using System.Text.Json.Serialization;

namespace Quill.AzureDevOps.Dto;

internal sealed class PullRequestWorkItemRefsResponse
{
    [JsonPropertyName("value")]
    public IReadOnlyList<PullRequestWorkItemRef> Value { get; init; } = Array.Empty<PullRequestWorkItemRef>();
}

internal sealed class PullRequestWorkItemRef
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; init; } = string.Empty;
}
