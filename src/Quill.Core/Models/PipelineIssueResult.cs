using System.Text.Json.Serialization;

namespace Quill.Core.Models;

public class PipelineIssueResult
{
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("message")]
    public required string Message { get; init; }

    [JsonPropertyName("line")]
    public int? Line { get; init; }
}
