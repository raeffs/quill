using System.Text.Json.Serialization;

namespace Quill.AzureDevOps.Dto;

internal sealed class TestResultResponse
{
    [JsonPropertyName("testCaseTitle")]
    public string? TestCaseTitle { get; init; }

    [JsonPropertyName("automatedTestName")]
    public string? AutomatedTestName { get; init; }

    [JsonPropertyName("automatedTestStorage")]
    public string? AutomatedTestStorage { get; init; }

    [JsonPropertyName("outcome")]
    public string Outcome { get; init; } = string.Empty;

    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; init; }

    [JsonPropertyName("stackTrace")]
    public string? StackTrace { get; init; }

    [JsonPropertyName("durationInMs")]
    public double? DurationInMs { get; init; }

    [JsonPropertyName("failingSince")]
    public FailingSinceResponse? FailingSince { get; init; }
}
