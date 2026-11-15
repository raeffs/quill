using System.Text.Json.Serialization;

namespace Quill.AzureDevOps.Dto;

internal sealed class TimelineRecordResponse
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("parentId")]
    public string? ParentId { get; init; }

    [JsonPropertyName("type")]
    public string Type { get; init; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("identifier")]
    public string? Identifier { get; init; }

    [JsonPropertyName("state")]
    public string State { get; init; } = string.Empty;

    [JsonPropertyName("result")]
    public string? Result { get; init; }

    [JsonPropertyName("order")]
    public int? Order { get; init; }

    [JsonPropertyName("errorCount")]
    public int? ErrorCount { get; init; }

    [JsonPropertyName("warningCount")]
    public int? WarningCount { get; init; }

    [JsonPropertyName("log")]
    public TimelineLogResponse? Log { get; init; }

    [JsonPropertyName("issues")]
    public IReadOnlyList<TimelineIssueResponse>? Issues { get; init; }
}
