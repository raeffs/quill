using System.Text.Json.Serialization;

namespace Quill.Core.Models;

public class PipelineResult
{
    [JsonPropertyName("id")]
    public required int Id { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("folder")]
    public required string Folder { get; init; }
}
