using System.Text.Json.Serialization;

namespace Quill.AzureDevOps.Dto;

internal sealed class TimelineLogResponse
{
    [JsonPropertyName("id")]
    public int? Id { get; init; }
}
