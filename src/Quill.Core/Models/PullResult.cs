using System.Text.Json.Serialization;

namespace Quill.Core.Models;

public class PullResult
{
    [JsonPropertyName("id")]
    public required int Id { get; init; }

    [JsonPropertyName("title")]
    public required string Title { get; init; }

    [JsonPropertyName("file")]
    public required string File { get; init; }
}
