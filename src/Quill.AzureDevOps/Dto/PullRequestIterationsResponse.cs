using System.Text.Json.Serialization;

namespace Quill.AzureDevOps.Dto;

internal sealed class PullRequestIterationsResponse
{
    [JsonPropertyName("value")]
    public IReadOnlyList<PullRequestIterationResponse> Value { get; init; } = Array.Empty<PullRequestIterationResponse>();
}
