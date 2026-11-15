using System.Text.Json.Serialization;

namespace Quill.AzureDevOps.Dto;

internal sealed class RunPipelineRepository
{
    [JsonPropertyName("refName")]
    public required string RefName { get; init; }
}
