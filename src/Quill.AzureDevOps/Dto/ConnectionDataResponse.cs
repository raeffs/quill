using System.Text.Json.Serialization;

namespace Quill.AzureDevOps.Dto;

internal sealed class ConnectionDataResponse
{
    [JsonPropertyName("authenticatedUser")]
    public required AuthenticatedUserResponse AuthenticatedUser { get; init; }
}
