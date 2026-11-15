using System.Text.Json.Serialization;

namespace Quill.AzureDevOps.Dto;

internal sealed class RunPipelineResources
{
    [JsonPropertyName("repositories")]
    public required RunPipelineRepositories Repositories { get; init; }
}
