using System.Text.Json.Serialization;

namespace Quill.AzureDevOps.Dto;

internal sealed class CreatedWorkItemResponse
{
    [JsonPropertyName("id")]
    public required int Id { get; init; }
}
