using System.Text.Json;
using Quill.Core;
using Quill.Core.Models;

namespace Quill.AzureDevOps;

public class AzureDevOpsIdentityClient : IIdentityClient
{
    private readonly HttpClient _httpClient;
    private readonly string _connectionDataUrl;

    public AzureDevOpsIdentityClient(HttpClient httpClient, string serverUrl, string collection)
    {
        _httpClient = httpClient;
        _connectionDataUrl = $"{serverUrl.TrimEnd('/')}/{collection}/_apis/connectionData";
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
}
