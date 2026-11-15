using System.Text.Json.Serialization;

namespace Quill.AzureDevOps.Dto;

internal sealed class PullRequestCommitRefResponse
{
    [JsonPropertyName("commitId")]
    public string CommitId { get; init; } = string.Empty;
}
