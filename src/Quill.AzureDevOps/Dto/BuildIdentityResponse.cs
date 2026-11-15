using System.Text.Json.Serialization;

namespace Quill.AzureDevOps.Dto;

internal sealed class BuildIdentityResponse
{
    [JsonPropertyName("displayName")]
    public string DisplayName { get; init; } = string.Empty;
}
