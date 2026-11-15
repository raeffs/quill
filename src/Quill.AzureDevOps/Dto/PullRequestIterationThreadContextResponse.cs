using System.Text.Json.Serialization;

namespace Quill.AzureDevOps.Dto;

internal sealed class PullRequestIterationThreadContextResponse
{
    [JsonPropertyName("iterationContext")]
    public PullRequestIterationContextResponse? IterationContext { get; init; }

    [JsonPropertyName("trackingCriteria")]
    public PullRequestTrackingCriteriaResponse? TrackingCriteria { get; init; }
}
