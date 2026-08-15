using System.Text.Json.Serialization;

namespace Quill.AzureDevOps.Dto;

internal sealed class PullRequestIterationsResponse
{
    [JsonPropertyName("value")]
    public IReadOnlyList<PullRequestIterationResponse> Value { get; init; } = Array.Empty<PullRequestIterationResponse>();
}

internal sealed class PullRequestIterationResponse
{
    [JsonPropertyName("id")]
    public int Id { get; init; }
}
