using System.Text.Json;
using System.Text.Json.Serialization;

namespace Quill.AzureDevOps.Dto;

internal sealed class PolicyEvaluationResponse
{
    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;

    [JsonPropertyName("startedDate")]
    public DateTimeOffset? StartedDate { get; init; }

    [JsonPropertyName("completedDate")]
    public DateTimeOffset? CompletedDate { get; init; }

    [JsonPropertyName("configuration")]
    public PolicyConfigurationResponse? Configuration { get; init; }

    [JsonPropertyName("context")]
    public JsonElement Context { get; init; }
}
