using System.Text.Json.Serialization;

namespace Quill.AzureDevOps.Dto;

internal sealed class PullRequestThreadContextResponse
{
    [JsonPropertyName("filePath")]
    public string? FilePath { get; init; }

    [JsonPropertyName("rightFileStart")]
    public PullRequestFilePosition? RightFileStart { get; init; }

    [JsonPropertyName("rightFileEnd")]
    public PullRequestFilePosition? RightFileEnd { get; init; }

    [JsonPropertyName("leftFileStart")]
    public PullRequestFilePosition? LeftFileStart { get; init; }

    [JsonPropertyName("leftFileEnd")]
    public PullRequestFilePosition? LeftFileEnd { get; init; }
}
