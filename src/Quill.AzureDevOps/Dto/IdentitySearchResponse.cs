using System.Text.Json;
using System.Text.Json.Serialization;

namespace Quill.AzureDevOps.Dto;

internal sealed class IdentitySearchResponse
{
    [JsonPropertyName("value")]
    public JsonElement Value { get; init; }
}
