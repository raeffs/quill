using System.Text.Json;
using Quill.AzureDevOps.Dto;
using Quill.Core;
using Quill.Core.Models;

namespace Quill.AzureDevOps;

public class AzureDevOpsIdentityClient(AzureDevOpsServer server) : IIdentityClient
{
    public async Task<CurrentUser> GetCurrentUserAsync()
    {
        var request = ServerRequest.Get(ServerScope.Collection, "_apis/connectionData") with { ApiVersion = null };
        var answer = await server.ReadAsync(request, AzureDevOpsJsonContext.Default.ConnectionDataResponse, CancellationToken.None);
        var authenticatedUser = answer.AuthenticatedUser;

        var id = authenticatedUser.Id
            ?? throw new DefectException("Authenticated user ID not found in connection data.");

        var displayName = authenticatedUser.ProviderDisplayName
            ?? throw new DefectException("Authenticated user display name not found in connection data.");

        return new CurrentUser(id, displayName);
    }

    public async Task<IReadOnlyList<IdentityMatch>> SearchAsync(
        IdentitySearchFilter filter, string value, CancellationToken cancellationToken)
    {
        var path = $"_apis/identities?searchFilter={filter}&filterValue={Uri.EscapeDataString(value)}&queryMembership=None";
        var request = ServerRequest.Get(ServerScope.Collection, path) with { ApiVersion = "7.1" };
        IdentitySearchResponse answer;
        try
        {
            answer = await server.ReadAsync(request, AzureDevOpsJsonContext.Default.IdentitySearchResponse, cancellationToken);
        }
        catch (DeniedException exception)
        {
            throw new DeniedException(
                $"{exception.Message} Resolving a person needs the Identity: Read PAT scope.",
                exception);
        }

        var matches = new List<IdentityMatch>();
        if (answer.Value.ValueKind != JsonValueKind.Array)
        {
            return matches;
        }

        foreach (var item in answer.Value.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var id = ReadString(item, "id")
                ?? throw new DefectException("Identity ID not found in identities response.");
            var displayName = ReadString(item, "providerDisplayName")
                ?? throw new DefectException("Identity display name not found in identities response.");
            var isContainer = item.TryGetProperty("isContainer", out var container) && container.ValueKind == JsonValueKind.True;
            var domain = ReadProperty(item, "Domain");
            var account = ReadProperty(item, "Account");
            var uniqueName = string.IsNullOrEmpty(domain) || string.IsNullOrEmpty(account) ? null : $@"{domain}\{account}";
            matches.Add(new IdentityMatch(id, displayName, uniqueName, isContainer));
        }

        return matches;
    }

    private static string? ReadString(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static string ReadProperty(JsonElement item, string name) =>
        item.TryGetProperty("properties", out var properties)
        && properties.ValueKind == JsonValueKind.Object
        && properties.TryGetProperty(name, out var property)
        && property.ValueKind == JsonValueKind.Object
        && property.TryGetProperty("$value", out var value)
        && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? string.Empty
            : string.Empty;
}
