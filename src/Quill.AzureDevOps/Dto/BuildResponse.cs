using System.Text.Json.Serialization;

namespace Quill.AzureDevOps.Dto;

internal sealed class BuildResponse
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("buildNumber")]
    public string BuildNumber { get; init; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;

    [JsonPropertyName("result")]
    public string? Result { get; init; }

    [JsonPropertyName("reason")]
    public string Reason { get; init; } = string.Empty;

    [JsonPropertyName("sourceBranch")]
    public string SourceBranch { get; init; } = string.Empty;

    [JsonPropertyName("sourceVersion")]
    public string SourceVersion { get; init; } = string.Empty;

    [JsonPropertyName("queueTime")]
    public DateTimeOffset QueueTime { get; init; }

    [JsonPropertyName("startTime")]
    public DateTimeOffset? StartTime { get; init; }

    [JsonPropertyName("finishTime")]
    public DateTimeOffset? FinishTime { get; init; }

    [JsonPropertyName("definition")]
    public BuildDefinitionResponse? Definition { get; init; }

    [JsonPropertyName("requestedFor")]
    public BuildIdentityResponse? RequestedFor { get; init; }

    [JsonPropertyName("_links")]
    public BuildLinksResponse? Links { get; init; }
}

internal sealed class BuildDefinitionResponse
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("path")]
    public string Path { get; init; } = string.Empty;
}

internal sealed class BuildIdentityResponse
{
    [JsonPropertyName("displayName")]
    public string DisplayName { get; init; } = string.Empty;
}

internal sealed class BuildLinksResponse
{
    [JsonPropertyName("web")]
    public BuildLinkResponse? Web { get; init; }
}

internal sealed class BuildLinkResponse
{
    [JsonPropertyName("href")]
    public string Href { get; init; } = string.Empty;
}

internal sealed class BuildDefinitionListResponse
{
    [JsonPropertyName("value")]
    public IReadOnlyList<BuildDefinitionResponse> Value { get; init; } = Array.Empty<BuildDefinitionResponse>();
}

internal sealed class BuildListResponse
{
    [JsonPropertyName("value")]
    public IReadOnlyList<BuildResponse> Value { get; init; } = Array.Empty<BuildResponse>();
}
