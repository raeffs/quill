using System.Text.Json.Serialization;

namespace Quill.AzureDevOps.Dto;

internal sealed class AuthenticatedUserResponse
{
    [JsonPropertyName("id")]
    public required string? Id { get; init; }

    [JsonPropertyName("providerDisplayName")]
    public required string? ProviderDisplayName { get; init; }
}
