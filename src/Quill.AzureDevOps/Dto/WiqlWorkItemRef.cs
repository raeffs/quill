using System.Text.Json.Serialization;

namespace Quill.AzureDevOps.Dto;

public class WiqlWorkItemRef
{
    [JsonPropertyName("id")]
    public int Id { get; init; }
}
