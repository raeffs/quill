using System.Text.Json.Serialization;

namespace Quill.AzureDevOps.Dto;

internal sealed class PullRequestItemResponse
{
    [JsonPropertyName("codeReviewId")]
    public int CodeReviewId { get; init; }

    [JsonPropertyName("pullRequestId")]
    public int PullRequestId { get; init; }

    [JsonPropertyName("title")]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;

    [JsonPropertyName("isDraft")]
    public bool IsDraft { get; init; }

    [JsonPropertyName("mergeStatus")]
    public string? MergeStatus { get; init; }

    [JsonPropertyName("lastMergeSourceCommit")]
    public PullRequestCommitRefResponse? LastMergeSourceCommit { get; init; }

    [JsonPropertyName("lastMergeTargetCommit")]
    public PullRequestCommitRefResponse? LastMergeTargetCommit { get; init; }

    [JsonPropertyName("labels")]
    public IReadOnlyList<PullRequestLabelResponse>? Labels { get; init; }

    [JsonPropertyName("sourceRefName")]
    public string SourceRefName { get; init; } = string.Empty;

    [JsonPropertyName("targetRefName")]
    public string TargetRefName { get; init; } = string.Empty;

    [JsonPropertyName("creationDate")]
    public DateTimeOffset CreationDate { get; init; }

    [JsonPropertyName("closedDate")]
    public DateTimeOffset? ClosedDate { get; init; }

    [JsonPropertyName("createdBy")]
    public PullRequestIdentityResponse? CreatedBy { get; init; }

    [JsonPropertyName("repository")]
    public PullRequestRepositoryResponse? Repository { get; init; }

    [JsonPropertyName("reviewers")]
    public IReadOnlyList<PullRequestReviewerResponse>? Reviewers { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }
}
