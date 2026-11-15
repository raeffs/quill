using System.Text.Json.Serialization;

namespace Quill.AzureDevOps.Dto;

internal sealed class FailingSinceBuildResponse
{
    [JsonPropertyName("id")]
    public int? Id { get; init; }
}
