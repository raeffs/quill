using System.Text.Json.Serialization;

namespace Quill.AzureDevOps.Dto;

internal sealed class PolicyConfigurationResponse
{
    [JsonPropertyName("isBlocking")]
    public bool IsBlocking { get; init; }

    [JsonPropertyName("type")]
    public PolicyTypeResponse? Type { get; init; }
}
