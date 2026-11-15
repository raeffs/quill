using System.Text.Json.Serialization;

namespace Quill.AzureDevOps.Dto;

internal sealed class PullRequestThreadsResponse
{
    [JsonPropertyName("value")]
    public IReadOnlyList<PullRequestThreadResponse> Value { get; init; } = Array.Empty<PullRequestThreadResponse>();
}
