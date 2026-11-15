using System.Text.Json.Serialization;

namespace Quill.AzureDevOps.Dto;

internal sealed class BuildLinkResponse
{
    [JsonPropertyName("href")]
    public string Href { get; init; } = string.Empty;
}
