using System.Text.Json.Serialization;

namespace Quill.AzureDevOps.Dto;

internal sealed class PullRequestListResponse
{
    [JsonPropertyName("value")]
    public IReadOnlyList<PullRequestItemResponse> Value { get; init; } = Array.Empty<PullRequestItemResponse>();
}
