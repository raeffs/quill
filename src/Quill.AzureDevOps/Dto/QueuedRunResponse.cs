using System.Text.Json.Serialization;

namespace Quill.AzureDevOps.Dto;

internal sealed class QueuedRunResponse
{
    [JsonPropertyName("id")]
    public int Id { get; init; }
}
