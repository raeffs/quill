using System.Text.Json.Serialization;

namespace Quill.AzureDevOps.Dto;

internal sealed class TestRunListResponse
{
    [JsonPropertyName("value")]
    public IReadOnlyList<TestRunResponse> Value { get; init; } = Array.Empty<TestRunResponse>();
}

internal sealed class TestRunResponse
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("state")]
    public string State { get; init; } = string.Empty;

    [JsonPropertyName("totalTests")]
    public int TotalTests { get; init; }

    [JsonPropertyName("passedTests")]
    public int PassedTests { get; init; }

    [JsonPropertyName("unanalyzedTests")]
    public int UnanalyzedTests { get; init; }

    [JsonPropertyName("notApplicableTests")]
    public int NotApplicableTests { get; init; }
}
