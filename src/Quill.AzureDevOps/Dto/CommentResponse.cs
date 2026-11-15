using System.Text.Json.Serialization;

namespace Quill.AzureDevOps.Dto;

public class CommentResponse
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("text")]
    public string Text { get; init; } = string.Empty;

    [JsonPropertyName("isDeleted")]
    public bool IsDeleted { get; init; }

    [JsonPropertyName("createdBy")]
    public CommentIdentity? CreatedBy { get; init; }

    [JsonPropertyName("createdDate")]
    public DateTimeOffset CreatedDate { get; init; }

    [JsonPropertyName("modifiedDate")]
    public DateTimeOffset? ModifiedDate { get; init; }
}
