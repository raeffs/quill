using System.Net;
using Quill.Tests.Shared;
using Shouldly;

namespace Quill.AzureDevOps.Tests;

public class AzureDevOpsIdentityClientTests
{
    [Fact]
    public async Task GetCurrentUserAsync_ValidResponse_ReturnsUserIdAndDisplayName()
    {
        // Arrange
        var connectionDataJson = """
        {
            "authenticatedUser": {
                "id": "6f1c2b7e-3d4a-4e5f-9a8b-0c1d2e3f4a5b",
                "providerDisplayName": "Test User"
            }
        }
        """;

        using var handler = new FakeHttpHandler(connectionDataJson, HttpStatusCode.OK);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsIdentityClient(httpClient, TestConstants.ServerUrl, TestConstants.Collection);

        // Act
        var currentUser = await client.GetCurrentUserAsync();

        // Assert
        currentUser.Id.ShouldBe("6f1c2b7e-3d4a-4e5f-9a8b-0c1d2e3f4a5b");
        currentUser.DisplayName.ShouldBe("Test User");
        handler.LastRequest!.RequestUri!.ToString().ShouldBe(
            $"{TestConstants.ServerUrl}/{TestConstants.Collection}/_apis/connectionData");
    }
}
