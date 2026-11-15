using System.Text.Json.Serialization;

namespace Quill.Core.Models;

public class PipelineStageResult
{
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("displayName")]
    public required string DisplayName { get; init; }

    [JsonPropertyName("state")]
    public required string State { get; init; }

    [JsonPropertyName("result")]
    public string? Result { get; init; }

    [JsonPropertyName("jobs")]
    public required IReadOnlyList<PipelineJobResult> Jobs { get; init; }
}
