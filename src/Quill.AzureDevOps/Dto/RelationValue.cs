using System.Text.Json.Serialization;

namespace Quill.AzureDevOps.Dto;

public class RelationValue
{
    [JsonPropertyName("rel")]
    public required string Rel { get; init; }

    [JsonPropertyName("url")]
    public required string Url { get; init; }
}
