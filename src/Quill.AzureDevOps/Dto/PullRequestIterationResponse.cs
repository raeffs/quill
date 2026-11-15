using System.Text.Json.Serialization;

namespace Quill.AzureDevOps.Dto;

internal sealed class PullRequestIterationResponse
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("createdDate")]
    public DateTimeOffset CreatedDate { get; init; }

    [JsonPropertyName("author")]
    public PullRequestIdentityResponse? Author { get; init; }

    [JsonPropertyName("sourceRefCommit")]
    public PullRequestCommitRefResponse? SourceRefCommit { get; init; }

    [JsonPropertyName("targetRefCommit")]
    public PullRequestCommitRefResponse? TargetRefCommit { get; init; }

    [JsonPropertyName("commonRefCommit")]
    public PullRequestCommitRefResponse? CommonRefCommit { get; init; }
}
