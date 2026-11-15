using System.Text.Json.Serialization;

namespace Quill.Core.Models;

public class PullRequestRevisionResult
{
    [JsonPropertyName("id")]
    public required int Id { get; init; }

    [JsonPropertyName("createdDate")]
    public required string CreatedDate { get; init; }

    [JsonPropertyName("author")]
    public string? Author { get; init; }

    [JsonPropertyName("sourceCommit")]
    public string? SourceCommit { get; init; }

    [JsonPropertyName("targetCommit")]
    public string? TargetCommit { get; init; }

    [JsonPropertyName("commonCommit")]
    public string? CommonCommit { get; init; }
}
