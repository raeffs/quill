using System.Text.Json.Serialization;

namespace Quill.AzureDevOps.Dto;

internal sealed class BuildLinksResponse
{
    [JsonPropertyName("web")]
    public BuildLinkResponse? Web { get; init; }
}
