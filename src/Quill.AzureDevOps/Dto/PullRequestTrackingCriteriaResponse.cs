using System.Text.Json.Serialization;

namespace Quill.AzureDevOps.Dto;

internal sealed class PullRequestTrackingCriteriaResponse
{
    [JsonPropertyName("origFilePath")]
    public string? OrigFilePath { get; init; }

    [JsonPropertyName("origRightFileStart")]
    public PullRequestFilePosition? OrigRightFileStart { get; init; }

    [JsonPropertyName("origRightFileEnd")]
    public PullRequestFilePosition? OrigRightFileEnd { get; init; }

    [JsonPropertyName("origLeftFileStart")]
    public PullRequestFilePosition? OrigLeftFileStart { get; init; }

    [JsonPropertyName("origLeftFileEnd")]
    public PullRequestFilePosition? OrigLeftFileEnd { get; init; }
}
