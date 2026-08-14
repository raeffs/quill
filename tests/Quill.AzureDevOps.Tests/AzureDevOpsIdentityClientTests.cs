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
                "id": "44892788-c082-4795-a323-8cc6daaaaba2",
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
        currentUser.Id.ShouldBe("44892788-c082-4795-a323-8cc6daaaaba2");
        currentUser.DisplayName.ShouldBe("Test User");
        handler.LastRequest!.RequestUri!.ToString().ShouldBe(
            $"{TestConstants.ServerUrl}/{TestConstants.Collection}/_apis/connectionData");
    }
}
