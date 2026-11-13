using System.Text.Json;
using Quill.Core;
using Quill.Core.Models;

namespace Quill.AzureDevOps;

public class AzureDevOpsIdentityClient : IIdentityClient
{
    private readonly HttpClient _httpClient;
    private readonly string _connectionDataUrl;
    private readonly string _identitiesUrl;

    public AzureDevOpsIdentityClient(HttpClient httpClient, string serverUrl, string collection)
    {
        _httpClient = httpClient;
        _connectionDataUrl = $"{serverUrl.TrimEnd('/')}/{collection}/_apis/connectionData";
        _identitiesUrl = $"{serverUrl.TrimEnd('/')}/{collection}/_apis/identities";
    }

    public async Task<CurrentUser> GetCurrentUserAsync()
    {
        var response = await _httpClient.GetAsync(_connectionDataUrl);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        var authenticatedUser = doc.RootElement.GetProperty("authenticatedUser");

        var id = authenticatedUser.GetProperty("id").GetString()
            ?? throw new DefectException("Authenticated user ID not found in connection data.");

        var displayName = authenticatedUser.GetProperty("providerDisplayName").GetString()
            ?? throw new DefectException("Authenticated user display name not found in connection data.");

        return new CurrentUser(id, displayName);
    }

    public async Task<IReadOnlyList<IdentityMatch>> SearchAsync(
        IdentitySearchFilter filter, string value, CancellationToken cancellationToken)
    {
        var url = $"{_identitiesUrl}?searchFilter={filter}&filterValue={Uri.EscapeDataString(value)}&queryMembership=None&api-version=7.1";
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.GetAsync(url, cancellationToken);
        }
        catch (DeniedException exception)
        {
            throw new DeniedException(
                $"{exception.Message} Resolving a person needs the Identity: Read PAT scope.",
                exception);
        }

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));

        var matches = new List<IdentityMatch>();
        if (!doc.RootElement.TryGetProperty("value", out var items) || items.ValueKind != JsonValueKind.Array)
        {
            return matches;
        }

        foreach (var item in items.EnumerateArray())
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
