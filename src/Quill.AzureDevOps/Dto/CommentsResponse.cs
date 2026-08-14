using System.Text.Json.Serialization;

namespace Quill.AzureDevOps.Dto;

public class CommentsResponse
{
    [JsonPropertyName("totalCount")]
    public int TotalCount { get; init; }

    [JsonPropertyName("count")]
    public int Count { get; init; }

    [JsonPropertyName("comments")]
    public IReadOnlyList<CommentResponse> Comments { get; init; } = Array.Empty<CommentResponse>();

    [JsonPropertyName("continuationToken")]
    public string? ContinuationToken { get; init; }
}

public class CommentResponse
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("text")]
    public string Text { get; init; } = string.Empty;

    [JsonPropertyName("isDeleted")]
    public bool IsDeleted { get; init; }

    [JsonPropertyName("createdBy")]
    public CommentIdentity? CreatedBy { get; init; }

    [JsonPropertyName("createdDate")]
    public DateTimeOffset CreatedDate { get; init; }

    [JsonPropertyName("modifiedDate")]
    public DateTimeOffset? ModifiedDate { get; init; }
}

public class CommentIdentity
{
    [JsonPropertyName("displayName")]
    public string? DisplayName { get; init; }
}
