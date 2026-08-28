using System.Text.Json.Serialization;

namespace Quill.AzureDevOps.Dto;

internal sealed class ApiErrorResponse
{
    [JsonPropertyName("message")]
    public string? Message { get; init; }
}
