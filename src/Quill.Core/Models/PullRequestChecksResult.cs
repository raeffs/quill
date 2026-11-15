using System.Text.Json.Serialization;

namespace Quill.Core.Models;

public class PullRequestChecksResult
{
    [JsonPropertyName("id")]
    public required int Id { get; init; }

    [JsonPropertyName("verdict")]
    public required string? Verdict { get; init; }

    [JsonPropertyName("status")]
    public required string Status { get; init; }

    [JsonPropertyName("mergeStatus")]
    public string? MergeStatus { get; init; }

    [JsonPropertyName("isDraft")]
    public required bool IsDraft { get; init; }

    [JsonPropertyName("checks")]
    public required IReadOnlyList<PullRequestCheckResult> Checks { get; init; }
}
