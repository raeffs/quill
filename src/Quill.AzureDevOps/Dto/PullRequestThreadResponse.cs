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

    [JsonPropertyName("threadContext")]
    public PullRequestThreadContextResponse? ThreadContext { get; init; }

    [JsonPropertyName("comments")]
    public IReadOnlyList<PullRequestThreadCommentResponse> Comments { get; init; } = Array.Empty<PullRequestThreadCommentResponse>();

    [JsonPropertyName("properties")]
    public JsonElement? Properties { get; init; }
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
    public string Content { get; init; } = string.Empty;

    [JsonPropertyName("isDeleted")]
    public bool IsDeleted { get; init; }

    [JsonPropertyName("author")]
    public PullRequestThreadAuthorResponse? Author { get; init; }

    [JsonPropertyName("publishedDate")]
    public DateTimeOffset PublishedDate { get; init; }

    [JsonPropertyName("lastContentUpdatedDate")]
    public DateTimeOffset? LastContentUpdatedDate { get; init; }
}

internal sealed class PullRequestThreadAuthorResponse
{
    [JsonPropertyName("displayName")]
    public string? DisplayName { get; init; }
}
