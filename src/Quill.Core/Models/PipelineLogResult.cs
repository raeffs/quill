using System.Text.Json.Serialization;

namespace Quill.Core.Models;

public class PipelineLogResult
{
    [JsonPropertyName("runId")]
    public required int RunId { get; init; }

    [JsonPropertyName("logId")]
    public required int LogId { get; init; }

    [JsonPropertyName("startLine")]
    public int? StartLine { get; init; }

    [JsonPropertyName("endLine")]
    public int? EndLine { get; init; }

    [JsonPropertyName("lines")]
    public required IReadOnlyList<string> Lines { get; init; }
}
