using System.Text.Json.Serialization;

namespace Quill.Core.Models;

public class SearchResult
{
    [JsonPropertyName("id")]
    public required int Id { get; init; }

    [JsonPropertyName("title")]
    public required string Title { get; init; }

    [JsonPropertyName("state")]
    public required string State { get; init; }

    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("assignedTo")]
    public string? AssignedTo { get; init; }

    [JsonPropertyName("parentId")]
    public int? ParentId { get; init; }
}
