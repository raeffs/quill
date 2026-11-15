using System.Text.Json.Serialization;

namespace Quill.AzureDevOps.Dto;

internal sealed class PullRequestProjectResponse
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;
}
