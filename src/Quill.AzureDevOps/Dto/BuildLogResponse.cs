using System.Text.Json.Serialization;

namespace Quill.AzureDevOps.Dto;

internal sealed class BuildLogResponse
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("lineCount")]
    public int LineCount { get; init; }
}
