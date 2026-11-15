using System.Text.Json.Serialization;

namespace Quill.Core.Models;

public class PullRequestCommentResult
{
    [JsonPropertyName("id")]
    public required int Id { get; init; }

    [JsonPropertyName("author")]
    public string? Author { get; init; }

    [JsonPropertyName("createdDate")]
    public required string CreatedDate { get; init; }

    [JsonPropertyName("modifiedDate")]
    public string? ModifiedDate { get; init; }

    [JsonPropertyName("lastUpdatedDate")]
    public required string LastUpdatedDate { get; init; }

    [JsonPropertyName("usersLiked")]
    public required IReadOnlyList<string> UsersLiked { get; init; }

    [JsonPropertyName("text")]
    public required string Text { get; init; }
}
