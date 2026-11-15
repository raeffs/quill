using System.Text.Json;
using System.Text.Json.Serialization;

namespace Quill.AzureDevOps.Dto;

public class WorkItemResponse
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("rev")]
    public int Rev { get; init; }

    [JsonPropertyName("fields")]
    public IReadOnlyDictionary<string, JsonElement> Fields { get; init; } = new Dictionary<string, JsonElement>(StringComparer.Ordinal);

    [JsonPropertyName("relations")]
    public IReadOnlyList<WorkItemRelationResponse>? Relations { get; init; }
}
