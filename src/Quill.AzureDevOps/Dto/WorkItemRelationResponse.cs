using System.Text.Json.Serialization;

namespace Quill.AzureDevOps.Dto;

public class WorkItemRelationResponse
{
    [JsonPropertyName("rel")]
    public string Rel { get; init; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; init; } = string.Empty;
}
