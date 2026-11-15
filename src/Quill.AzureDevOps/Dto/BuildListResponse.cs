using System.Text.Json.Serialization;

namespace Quill.AzureDevOps.Dto;

internal sealed class BuildListResponse
{
    [JsonPropertyName("value")]
    public IReadOnlyList<BuildResponse> Value { get; init; } = Array.Empty<BuildResponse>();
}
