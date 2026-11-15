using System.Text.Json.Serialization;

namespace Quill.AzureDevOps.Dto;

internal sealed class BuildLogLinesResponse
{
    [JsonPropertyName("value")]
    public IReadOnlyList<string> Value { get; init; } = Array.Empty<string>();
}
