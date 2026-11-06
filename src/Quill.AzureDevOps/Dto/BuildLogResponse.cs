using System.Text.Json.Serialization;

namespace Quill.AzureDevOps.Dto;

internal sealed class BuildLogLinesResponse
{
    [JsonPropertyName("value")]
    public IReadOnlyList<string> Value { get; init; } = Array.Empty<string>();
}

internal sealed class BuildLogListResponse
{
    [JsonPropertyName("value")]
    public IReadOnlyList<BuildLogResponse> Value { get; init; } = Array.Empty<BuildLogResponse>();
}

internal sealed class BuildLogResponse
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("lineCount")]
    public int LineCount { get; init; }
}
