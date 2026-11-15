using System.Text.Json.Serialization;

namespace Quill.AzureDevOps.Dto;

internal sealed class BuildDefinitionListResponse
{
    [JsonPropertyName("value")]
    public IReadOnlyList<BuildDefinitionResponse> Value { get; init; } = Array.Empty<BuildDefinitionResponse>();
}
