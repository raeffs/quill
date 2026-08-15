using System.Text.Json;
using System.Text.Json.Serialization;

namespace Quill.AzureDevOps.Dto;

internal sealed class PullRequestThreadsResponse
{
    [JsonPropertyName("value")]
    public IReadOnlyList<PullRequestThreadResponse> Value { get; init; } = Array.Empty<PullRequestThreadResponse>();
}

internal sealed class PullRequestThreadResponse
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("status")]
    public string? Status { get; init; }

    [JsonPropertyName("isDeleted")]
    public bool IsDeleted { get; init; }

    [JsonPropertyName("publishedDate")]
    public DateTimeOffset PublishedDate { get; init; }

    [JsonPropertyName("lastUpdatedDate")]
    public DateTimeOffset? LastUpdatedDate { get; init; }

    [JsonPropertyName("threadContext")]
    public PullRequestThreadContextResponse? ThreadContext { get; init; }

    [JsonPropertyName("pullRequestThreadContext")]
    public PullRequestIterationThreadContextResponse? PullRequestThreadContext { get; init; }

    [JsonPropertyName("comments")]
    public IReadOnlyList<PullRequestThreadCommentResponse>? Comments { get; init; }

    [JsonPropertyName("properties")]
    public JsonElement? Properties { get; init; }
}

internal sealed class PullRequestIterationThreadContextResponse
{
    [JsonPropertyName("iterationContext")]
    public PullRequestIterationContextResponse? IterationContext { get; init; }

    [JsonPropertyName("trackingCriteria")]
    public PullRequestTrackingCriteriaResponse? TrackingCriteria { get; init; }
}

internal sealed class PullRequestIterationContextResponse
{
    [JsonPropertyName("firstComparingIteration")]
    public int FirstComparingIteration { get; init; }

    [JsonPropertyName("secondComparingIteration")]
    public int SecondComparingIteration { get; init; }
}

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

internal sealed class PullRequestFilePosition
{
    [JsonPropertyName("line")]
    public int Line { get; init; }

    [JsonPropertyName("offset")]
    public int Offset { get; init; }
}

internal sealed class PullRequestThreadCommentResponse
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("content")]
    public string? Content { get; init; }

    [JsonPropertyName("isDeleted")]
    public bool IsDeleted { get; init; }

    [JsonPropertyName("author")]
    public PullRequestThreadAuthorResponse? Author { get; init; }

    [JsonPropertyName("publishedDate")]
    public DateTimeOffset PublishedDate { get; init; }

    [JsonPropertyName("lastContentUpdatedDate")]
    public DateTimeOffset? LastContentUpdatedDate { get; init; }

    [JsonPropertyName("lastUpdatedDate")]
    public DateTimeOffset? LastUpdatedDate { get; init; }

    [JsonPropertyName("usersLiked")]
    public IReadOnlyList<PullRequestThreadAuthorResponse>? UsersLiked { get; init; }
}

internal sealed class PullRequestThreadAuthorResponse
{
    [JsonPropertyName("displayName")]
    public string? DisplayName { get; init; }
}
