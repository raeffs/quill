using System.Text.Json.Serialization;

namespace Quill.AzureDevOps.Dto;

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
