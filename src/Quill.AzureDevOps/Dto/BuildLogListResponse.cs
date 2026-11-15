using System.Text.Json.Serialization;

namespace Quill.AzureDevOps.Dto;

internal sealed class BuildLogListResponse
{
    [JsonPropertyName("value")]
    public IReadOnlyList<BuildLogResponse> Value { get; init; } = Array.Empty<BuildLogResponse>();
}
