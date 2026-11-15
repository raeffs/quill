using System.Text.Json.Serialization;

namespace Quill.Core.Models;

public class PushResult
{
    [JsonPropertyName("id")]
    public required int Id { get; init; }

    [JsonPropertyName("title")]
    public required string Title { get; init; }

    [JsonPropertyName("updatedFields")]
    public required IReadOnlyList<string> UpdatedFields { get; init; }

    [JsonPropertyName("relationsAdded")]
    public required IReadOnlyList<int> RelationsAdded { get; init; }
}
