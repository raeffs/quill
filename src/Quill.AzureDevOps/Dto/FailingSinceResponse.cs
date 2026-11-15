using System.Text.Json.Serialization;

namespace Quill.AzureDevOps.Dto;

internal sealed class FailingSinceResponse
{
    [JsonPropertyName("date")]
    public DateTimeOffset? Date { get; init; }

    [JsonPropertyName("build")]
    public FailingSinceBuildResponse? Build { get; init; }
}
