using System.Text.Json.Serialization;

namespace Quill.AzureDevOps.Dto;

public class CommentIdentity
{
    [JsonPropertyName("displayName")]
    public string? DisplayName { get; init; }
}
