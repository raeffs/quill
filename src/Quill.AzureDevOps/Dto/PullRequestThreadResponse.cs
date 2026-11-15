using System.Text.Json;
using System.Text.Json.Serialization;

namespace Quill.AzureDevOps.Dto;

internal sealed class PullRequestThreadResponse
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("status")]
    public string? Status { get; init; }

    [JsonPropertyName("isDeleted")]
    public bool IsDeleted { get; init; }

    [JsonPropertyName("publishedDate")]
    public DateTimeOffset PublishedDate { get; init; }

    [JsonPropertyName("lastUpdatedDate")]
    public DateTimeOffset? LastUpdatedDate { get; init; }

    [JsonPropertyName("threadContext")]
    public PullRequestThreadContextResponse? ThreadContext { get; init; }

    [JsonPropertyName("pullRequestThreadContext")]
    public PullRequestIterationThreadContextResponse? PullRequestThreadContext { get; init; }

    [JsonPropertyName("comments")]
    public IReadOnlyList<PullRequestThreadCommentResponse>? Comments { get; init; }

    [JsonPropertyName("properties")]
    public JsonElement? Properties { get; init; }
}
