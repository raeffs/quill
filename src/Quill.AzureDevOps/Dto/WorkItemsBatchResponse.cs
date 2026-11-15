using System.Text.Json.Serialization;

namespace Quill.AzureDevOps.Dto;

public class WorkItemsBatchResponse
{
    [JsonPropertyName("count")]
    public int Count { get; init; }

    [JsonPropertyName("value")]
    public IReadOnlyList<WorkItemResponse?> Value { get; init; } = Array.Empty<WorkItemResponse?>();
}
