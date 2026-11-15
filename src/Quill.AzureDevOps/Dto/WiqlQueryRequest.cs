using System.Text.Json.Serialization;

namespace Quill.AzureDevOps.Dto;

public class WiqlQueryRequest
{
    [JsonPropertyName("query")]
    public required string Query { get; init; }
}
