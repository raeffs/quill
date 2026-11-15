using System.Text.Json.Serialization;

namespace Quill.Core.Models;

public class PipelineTestRunResult
{
    [JsonPropertyName("id")]
    public required int Id { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("state")]
    public required string State { get; init; }

    [JsonPropertyName("total")]
    public required int Total { get; init; }

    [JsonPropertyName("passed")]
    public required int Passed { get; init; }

    [JsonPropertyName("failed")]
    public required int Failed { get; init; }

    [JsonPropertyName("skipped")]
    public required int Skipped { get; init; }

    [JsonPropertyName("results")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<PipelineTestCaseResult>? Results { get; init; }
}
