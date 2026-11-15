using System.Text.Json.Serialization;

namespace Quill.AzureDevOps.Dto;

internal sealed class CreatePullRequestWorkItemRef
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }
}
