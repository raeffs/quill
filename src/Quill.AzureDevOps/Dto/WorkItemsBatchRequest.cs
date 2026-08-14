using System.Text.Json.Serialization;

namespace Quill.AzureDevOps.Dto;

public class WorkItemsBatchRequest
{
    [JsonPropertyName("ids")]
    public required IReadOnlyList<int> Ids { get; init; }

    [JsonPropertyName("$expand")]
    public string Expand { get; init; } = "relations";

    [JsonPropertyName("errorPolicy")]
    public string ErrorPolicy { get; init; } = "omit";
}

public class WorkItemsBatchResponse
{
    [JsonPropertyName("count")]
    public int Count { get; init; }

    [JsonPropertyName("value")]
    public IReadOnlyList<WorkItemResponse> Value { get; init; } = Array.Empty<WorkItemResponse>();
}
