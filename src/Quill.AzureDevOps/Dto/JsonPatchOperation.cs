using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Quill.AzureDevOps.Dto;

public class JsonPatchOperation
{
    [JsonPropertyName("op")]
    public required string Op { get; init; }

    [JsonPropertyName("path")]
    public required string Path { get; init; }

    [JsonPropertyName("value")]
    public JsonNode? Value { get; init; }
}

public class RelationValue
{
    [JsonPropertyName("rel")]
    public required string Rel { get; init; }

    [JsonPropertyName("url")]
    public required string Url { get; init; }
}
