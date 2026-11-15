using System.Text.Json.Serialization;

namespace Quill.Core.Models;

public class ViewResult
{
    [JsonPropertyName("id")]
    public required int Id { get; init; }

    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("title")]
    public required string Title { get; init; }

    [JsonPropertyName("state")]
    public required string State { get; init; }

    [JsonPropertyName("assignedTo")]
    public string? AssignedTo { get; init; }

    [JsonPropertyName("parentId")]
    public int? ParentId { get; init; }

    [JsonPropertyName("description")]
    public required string Description { get; init; }

    [JsonPropertyName("relatedIds")]
    public required IReadOnlyList<int> RelatedIds { get; init; }

    [JsonPropertyName("tags")]
    public required IReadOnlyList<string> Tags { get; init; }

    [JsonPropertyName("predecessors")]
    public required IReadOnlyList<int> Predecessors { get; init; }

    [JsonPropertyName("children")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<ChildItem>? Children { get; init; }
}
