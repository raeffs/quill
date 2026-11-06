using System.Text.Json.Serialization;

namespace Quill.AzureDevOps.Dto;

internal sealed class RunPipelineBody
{
    [JsonPropertyName("resources")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public RunPipelineResources? Resources { get; init; }

    [JsonPropertyName("templateParameters")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyDictionary<string, string>? TemplateParameters { get; init; }

    [JsonPropertyName("stagesToSkip")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<string>? StagesToSkip { get; init; }
}

internal sealed class RunPipelineResources
{
    [JsonPropertyName("repositories")]
    public required RunPipelineRepositories Repositories { get; init; }
}

internal sealed class RunPipelineRepositories
{
    [JsonPropertyName("self")]
    public required RunPipelineRepository Self { get; init; }
}

internal sealed class RunPipelineRepository
{
    [JsonPropertyName("refName")]
    public required string RefName { get; init; }
}

internal sealed class QueuedRunResponse
{
    [JsonPropertyName("id")]
    public int Id { get; init; }
}
