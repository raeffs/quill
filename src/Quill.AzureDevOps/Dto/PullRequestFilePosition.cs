using System.Text.Json.Serialization;

namespace Quill.AzureDevOps.Dto;

internal sealed class PullRequestFilePosition
{
    [JsonPropertyName("line")]
    public int Line { get; init; }

    [JsonPropertyName("offset")]
    public int Offset { get; init; }
}
