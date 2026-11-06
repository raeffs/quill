using System.Text.Json.Serialization;

namespace Quill.AzureDevOps.Dto;

internal sealed class UpdateBuildBody
{
    [JsonPropertyName("status")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Status { get; init; }
}
