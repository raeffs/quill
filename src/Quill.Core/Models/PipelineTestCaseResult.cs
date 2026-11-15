using System.Text.Json.Serialization;

namespace Quill.Core.Models;

public class PipelineTestCaseResult
{
    [JsonPropertyName("title")]
    public string? Title { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("storage")]
    public string? Storage { get; init; }

    [JsonPropertyName("outcome")]
    public required string Outcome { get; init; }

    [JsonPropertyName("message")]
    public string? Message { get; init; }

    [JsonPropertyName("stackTrace")]
    public string? StackTrace { get; init; }

    [JsonPropertyName("durationMs")]
    public int? DurationMs { get; init; }

    [JsonPropertyName("failingSince")]
    public string? FailingSince { get; init; }

    [JsonPropertyName("failingSinceRunId")]
    public int? FailingSinceRunId { get; init; }
}
