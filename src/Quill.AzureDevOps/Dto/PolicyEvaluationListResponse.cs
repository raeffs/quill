using System.Text.Json.Serialization;

namespace Quill.AzureDevOps.Dto;

internal sealed class PolicyEvaluationListResponse
{
    [JsonPropertyName("value")]
    public IReadOnlyList<PolicyEvaluationResponse> Value { get; init; } = Array.Empty<PolicyEvaluationResponse>();
}
