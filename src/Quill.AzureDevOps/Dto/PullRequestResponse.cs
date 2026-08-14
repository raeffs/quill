using System.Text.Json.Serialization;

namespace Quill.AzureDevOps.Dto;

internal sealed class PullRequestListResponse
{
    [JsonPropertyName("value")]
    public IReadOnlyList<PullRequestItemResponse> Value { get; init; } = Array.Empty<PullRequestItemResponse>();
}

internal sealed class PullRequestItemResponse
{
    [JsonPropertyName("pullRequestId")]
    public int PullRequestId { get; init; }

    [JsonPropertyName("title")]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;

    [JsonPropertyName("isDraft")]
    public bool IsDraft { get; init; }

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
    public IReadOnlyList<PullRequestReviewerResponse> Reviewers { get; init; } = Array.Empty<PullRequestReviewerResponse>();

    [JsonPropertyName("description")]
    public string? Description { get; init; }
}

internal sealed class PullRequestIdentityResponse
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("displayName")]
    public string DisplayName { get; init; } = string.Empty;
}

internal sealed class PullRequestRepositoryResponse
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;
}

internal sealed class PullRequestReviewerResponse
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("displayName")]
    public string DisplayName { get; init; } = string.Empty;

    [JsonPropertyName("vote")]
    public int Vote { get; init; }

    [JsonPropertyName("isRequired")]
    public bool IsRequired { get; init; }
}
