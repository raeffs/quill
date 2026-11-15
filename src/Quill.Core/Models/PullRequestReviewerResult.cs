using System.Text.Json.Serialization;

namespace Quill.Core.Models;

public class PullRequestReviewerResult
{
    [JsonPropertyName("displayName")]
    public required string DisplayName { get; init; }

    [JsonPropertyName("vote")]
    public required string Vote { get; init; }

    [JsonPropertyName("isRequired")]
    public required bool IsRequired { get; init; }

    [JsonPropertyName("isContainer")]
    public required bool IsContainer { get; init; }
}
