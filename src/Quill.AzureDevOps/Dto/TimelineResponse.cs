using System.Text.Json.Serialization;

namespace Quill.AzureDevOps.Dto;

internal sealed class TimelineResponse
{
    [JsonPropertyName("records")]
    public IReadOnlyList<TimelineRecordResponse> Records { get; init; } = Array.Empty<TimelineRecordResponse>();
}
