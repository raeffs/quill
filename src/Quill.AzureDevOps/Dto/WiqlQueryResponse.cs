using System.Text.Json.Serialization;

namespace Quill.AzureDevOps.Dto;

public class WiqlQueryResponse
{
    [JsonPropertyName("workItems")]
    public IReadOnlyList<WiqlWorkItemRef> WorkItems { get; init; } = Array.Empty<WiqlWorkItemRef>();
}
