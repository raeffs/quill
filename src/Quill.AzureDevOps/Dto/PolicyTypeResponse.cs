using System.Text.Json.Serialization;

namespace Quill.AzureDevOps.Dto;

internal sealed class PolicyTypeResponse
{
    [JsonPropertyName("displayName")]
    public string DisplayName { get; init; } = string.Empty;
}
