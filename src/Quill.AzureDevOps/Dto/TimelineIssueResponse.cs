using System.Text.Json.Serialization;

namespace Quill.AzureDevOps.Dto;

internal sealed class TimelineIssueResponse
{
    [JsonPropertyName("type")]
    public string Type { get; init; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; init; } = string.Empty;

    [JsonPropertyName("data")]
    public Dictionary<string, string>? Data { get; init; }
}
