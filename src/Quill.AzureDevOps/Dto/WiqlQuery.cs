using System.Text.Json.Serialization;

namespace Quill.AzureDevOps.Dto;

public class WiqlQueryRequest
{
    [JsonPropertyName("query")]
    public required string Query { get; init; }
}

public class WiqlQueryResponse
{
    [JsonPropertyName("workItems")]
    public IReadOnlyList<WiqlWorkItemRef> WorkItems { get; init; } = Array.Empty<WiqlWorkItemRef>();
}

public class WiqlWorkItemRef
{
    [JsonPropertyName("id")]
    public int Id { get; init; }
}
