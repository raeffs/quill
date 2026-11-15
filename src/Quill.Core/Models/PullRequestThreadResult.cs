using System.Text.Json.Serialization;

namespace Quill.Core.Models;

public class PullRequestThreadResult
{
    [JsonPropertyName("id")]
    public required int Id { get; init; }

    [JsonPropertyName("status")]
    public required string Status { get; init; }

    [JsonPropertyName("filePath")]
    public string? FilePath { get; init; }

    [JsonPropertyName("side")]
    public string? Side { get; init; }

    [JsonPropertyName("startLine")]
    public int? StartLine { get; init; }

    [JsonPropertyName("endLine")]
    public int? EndLine { get; init; }

    [JsonPropertyName("positionState")]
    public string? PositionState { get; init; }

    [JsonPropertyName("origFilePath")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? OrigFilePath { get; init; }

    [JsonPropertyName("origStartLine")]
    public int? OrigStartLine { get; init; }

    [JsonPropertyName("origEndLine")]
    public int? OrigEndLine { get; init; }

    [JsonPropertyName("origStartColumn")]
    public int? OrigStartColumn { get; init; }

    [JsonPropertyName("origEndColumn")]
    public int? OrigEndColumn { get; init; }

    [JsonPropertyName("publishedDate")]
    public required string PublishedDate { get; init; }

    [JsonPropertyName("lastUpdatedDate")]
    public required string LastUpdatedDate { get; init; }

    [JsonPropertyName("comments")]
    public required IReadOnlyList<PullRequestCommentResult> Comments { get; init; }
}
