using System.Text.Json.Serialization;

namespace Quill.AzureDevOps.Dto;

internal sealed class RunPipelineRepositories
{
    [JsonPropertyName("self")]
    public required RunPipelineRepository Self { get; init; }
}
