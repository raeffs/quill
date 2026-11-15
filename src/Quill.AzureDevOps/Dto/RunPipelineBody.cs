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
