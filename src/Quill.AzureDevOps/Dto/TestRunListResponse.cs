using System.Text.Json.Serialization;

namespace Quill.AzureDevOps.Dto;

internal sealed class TestRunListResponse
{
    [JsonPropertyName("value")]
    public IReadOnlyList<TestRunResponse> Value { get; init; } = Array.Empty<TestRunResponse>();
}
