using System.Text.Json.Serialization;

namespace Quill.AzureDevOps.Dto;

internal sealed class CreatePullRequestBody
{
    [JsonPropertyName("sourceRefName")]
    public required string SourceRefName { get; init; }

    [JsonPropertyName("targetRefName")]
    public required string TargetRefName { get; init; }

    [JsonPropertyName("title")]
    public required string Title { get; init; }

    [JsonPropertyName("isDraft")]
    public required bool IsDraft { get; init; }

    [JsonPropertyName("description")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description { get; init; }

    [JsonPropertyName("workItemRefs")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<CreatePullRequestWorkItemRef>? WorkItemRefs { get; init; }
}

internal sealed class CreatePullRequestWorkItemRef
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }
}
