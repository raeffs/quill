using System.Text.Json.Serialization;

namespace Quill.Core.Models;

public class CommentResult
{
    [JsonPropertyName("id")]
    public required int Id { get; init; }

    [JsonPropertyName("author")]
    public string? Author { get; init; }

    [JsonPropertyName("createdDate")]
    public required string CreatedDate { get; init; }

    [JsonPropertyName("modifiedDate")]
    public string? ModifiedDate { get; init; }

    [JsonPropertyName("text")]
    public required string Text { get; init; }
}
