using System.Text.Json.Serialization;

namespace Quill.Core.Models;

public class PullRequestViewResult
{
    [JsonPropertyName("id")]
    public required int Id { get; init; }

    [JsonPropertyName("title")]
    public required string Title { get; init; }

    [JsonPropertyName("author")]
    public required string Author { get; init; }

    [JsonPropertyName("state")]
    public required string State { get; init; }

    [JsonPropertyName("isDraft")]
    public required bool IsDraft { get; init; }

    [JsonPropertyName("repo")]
    public required string Repo { get; init; }

    [JsonPropertyName("url")]
    public required string Url { get; init; }

    [JsonPropertyName("sourceBranch")]
    public required string SourceBranch { get; init; }

    [JsonPropertyName("targetBranch")]
    public required string TargetBranch { get; init; }

    [JsonPropertyName("createdDate")]
    public required string CreatedDate { get; init; }

    [JsonPropertyName("closedDate")]
    public string? ClosedDate { get; init; }

    [JsonPropertyName("mergeStatus")]
    public string? MergeStatus { get; init; }

    [JsonPropertyName("lastMergeSourceCommit")]
    public string? LastMergeSourceCommit { get; init; }

    [JsonPropertyName("lastMergeTargetCommit")]
    public string? LastMergeTargetCommit { get; init; }

    [JsonPropertyName("labels")]
    public required IReadOnlyList<string> Labels { get; init; }

    [JsonPropertyName("votes")]
    public required PullRequestVoteCountsResult Votes { get; init; }

    [JsonPropertyName("reviewers")]
    public required IReadOnlyList<PullRequestReviewerResult> Reviewers { get; init; }

    [JsonPropertyName("myVote")]
    public string? MyVote { get; init; }

    [JsonPropertyName("myIsRequired")]
    public bool? MyIsRequired { get; init; }

    [JsonPropertyName("description")]
    public required string Description { get; init; }

    [JsonPropertyName("workItems")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<PullRequestLinkedWorkItemResult>? WorkItems { get; init; }

    [JsonPropertyName("threads")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<PullRequestThreadResult>? Threads { get; init; }
}
