using System.Text.Json.Serialization;

namespace Quill.Core.Models;

public class PipelineJobResult
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("state")]
    public required string State { get; init; }

    [JsonPropertyName("result")]
    public string? Result { get; init; }

    [JsonPropertyName("logId")]
    public int? LogId { get; init; }

    [JsonPropertyName("errorCount")]
    public required int ErrorCount { get; init; }

    [JsonPropertyName("warningCount")]
    public required int WarningCount { get; init; }

    [JsonPropertyName("issues")]
    public required IReadOnlyList<PipelineIssueResult> Issues { get; init; }

    [JsonPropertyName("tasks")]
    public required IReadOnlyList<PipelineTaskResult> Tasks { get; init; }
}
