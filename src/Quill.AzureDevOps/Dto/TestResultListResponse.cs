using System.Text.Json.Serialization;

namespace Quill.AzureDevOps.Dto;

internal sealed class TestResultListResponse
{
    [JsonPropertyName("value")]
    public IReadOnlyList<TestResultResponse> Value { get; init; } = Array.Empty<TestResultResponse>();
}
