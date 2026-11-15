using System.Text.Json;
using System.Text.Json.Serialization;

namespace Quill.Core.Models;

public class PullRequestCheckResult
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("isBlocking")]
    public required bool IsBlocking { get; init; }

    [JsonPropertyName("status")]
    public required string Status { get; init; }

    [JsonPropertyName("startedDate")]
    public string? StartedDate { get; init; }

    [JsonPropertyName("completedDate")]
    public string? CompletedDate { get; init; }

    [JsonPropertyName("context")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public JsonElement? Context { get; init; }
}
