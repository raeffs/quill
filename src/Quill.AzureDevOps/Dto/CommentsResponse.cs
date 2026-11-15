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
