using System.Net;
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
        response.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        var authenticatedUser = doc.RootElement.GetProperty("authenticatedUser");

        var id = authenticatedUser.GetProperty("id").GetString()
            ?? throw new InvalidOperationException("Authenticated user ID not found in connection data.");

        var displayName = authenticatedUser.GetProperty("providerDisplayName").GetString()
            ?? throw new InvalidOperationException("Authenticated user display name not found in connection data.");

        return new CurrentUser(id, displayName);
    }

    public async Task<IReadOnlyList<IdentityMatch>> SearchAsync(
        IdentitySearchFilter filter, string value, CancellationToken cancellationToken)
    {
        var url = $"{_identitiesUrl}?searchFilter={filter}&filterValue={Uri.EscapeDataString(value)}&queryMembership=None&api-version=7.1";
        var response = await _httpClient.GetAsync(url, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new HttpRequestException(
                "Response status code does not indicate success: 401 (Unauthorized). Resolving a person needs the Identity: Read PAT scope.",
                null,
                HttpStatusCode.Unauthorized);
        }

        response.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));

        var matches = new List<IdentityMatch>();
        foreach (var item in doc.RootElement.GetProperty("value").EnumerateArray())
        {
            var id = item.GetProperty("id").GetString() ?? string.Empty;
            var displayName = item.GetProperty("providerDisplayName").GetString() ?? string.Empty;
            var isContainer = item.TryGetProperty("isContainer", out var container) && container.ValueKind == JsonValueKind.True;
            var properties = item.GetProperty("properties");
            var domain = ReadProperty(properties, "Domain");
            var account = ReadProperty(properties, "Account");
            var uniqueName = string.IsNullOrEmpty(domain) ? account : $@"{domain}\{account}";
            matches.Add(new IdentityMatch(id, displayName, uniqueName, isContainer));
        }

        return matches;
    }

    private static string ReadProperty(JsonElement properties, string name) =>
        properties.TryGetProperty(name, out var property) && property.TryGetProperty("$value", out var value)
            ? value.GetString() ?? string.Empty
            : string.Empty;
}
