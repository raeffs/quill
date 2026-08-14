using System.Text.Json;
using System.Text.Json.Serialization;

namespace Quill.AzureDevOps.Dto;

public class WorkItemResponse
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("fields")]
    public IReadOnlyDictionary<string, JsonElement> Fields { get; init; } = new Dictionary<string, JsonElement>(StringComparer.Ordinal);

    [JsonPropertyName("relations")]
    public IReadOnlyList<WorkItemRelationResponse>? Relations { get; init; }
}

public class WorkItemRelationResponse
{
    [JsonPropertyName("rel")]
    public string Rel { get; init; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; init; } = string.Empty;
}
