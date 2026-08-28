using System.Text.Json.Serialization;

namespace Quill.AzureDevOps.Dto;

internal sealed class GitRepositoryResponse
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("defaultBranch")]
    public string? DefaultBranch { get; init; }
}
