using System.Text.Json.Serialization;

namespace Quill.Core.Models;

public class ChildItem
{
    [JsonPropertyName("id")]
    public required int Id { get; init; }

    [JsonPropertyName("title")]
    public required string Title { get; init; }

    [JsonPropertyName("state")]
    public required string State { get; init; }
}
