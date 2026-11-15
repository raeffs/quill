using System.Text.Json.Serialization;

namespace Quill.Core.Models;

public class PipelineRunResult
{
    [JsonPropertyName("id")]
    public required int Id { get; init; }

    [JsonPropertyName("pipelineId")]
    public required int PipelineId { get; init; }

    [JsonPropertyName("pipelineName")]
    public required string PipelineName { get; init; }

    [JsonPropertyName("buildNumber")]
    public required string BuildNumber { get; init; }

    [JsonPropertyName("status")]
    public required string Status { get; init; }

    [JsonPropertyName("result")]
    public string? Result { get; init; }

    [JsonPropertyName("reason")]
    public required string Reason { get; init; }

    [JsonPropertyName("sourceBranch")]
    public required string SourceBranch { get; init; }

    [JsonPropertyName("sourceVersion")]
    public required string SourceVersion { get; init; }

    [JsonPropertyName("requestedFor")]
    public required string RequestedFor { get; init; }

    [JsonPropertyName("queueTime")]
    public required string QueueTime { get; init; }

    [JsonPropertyName("startTime")]
    public string? StartTime { get; init; }

    [JsonPropertyName("finishTime")]
    public string? FinishTime { get; init; }

    [JsonPropertyName("url")]
    public required string Url { get; init; }

    [JsonPropertyName("stages")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<PipelineStageResult>? Stages { get; init; }
}
