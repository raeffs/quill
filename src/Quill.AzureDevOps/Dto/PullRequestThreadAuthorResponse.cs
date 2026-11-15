using System.Text.Json.Serialization;

namespace Quill.AzureDevOps.Dto;

internal sealed class PullRequestThreadAuthorResponse
{
    [JsonPropertyName("displayName")]
    public string? DisplayName { get; init; }
}
