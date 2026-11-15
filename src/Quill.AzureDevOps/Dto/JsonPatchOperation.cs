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
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public JsonNode? Value { get; init; }
}
