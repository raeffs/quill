using System.Text.Json.Serialization;

namespace Quill.AzureDevOps.Dto;

internal sealed class BuildDefinitionResponse
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("path")]
    public string Path { get; init; } = string.Empty;
}
