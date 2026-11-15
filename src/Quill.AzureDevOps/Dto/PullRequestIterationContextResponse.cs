using System.Text.Json.Serialization;

namespace Quill.AzureDevOps.Dto;

internal sealed class PullRequestIterationContextResponse
{
    [JsonPropertyName("firstComparingIteration")]
    public int FirstComparingIteration { get; init; }

    [JsonPropertyName("secondComparingIteration")]
    public int SecondComparingIteration { get; init; }
}
