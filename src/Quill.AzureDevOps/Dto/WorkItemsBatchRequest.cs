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
