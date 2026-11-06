using System.Net;
using System.Net.Http.Headers;
using Quill.Core;
using Quill.Tests.Shared;
using Shouldly;

namespace Quill.AzureDevOps.Tests;

public class AzureDevOpsBuildClientRetryAfterTests
{
    [Fact]
    public async Task GetRunAsync_429WithRetryAfterSeconds_ThrowsWithTheDelay()
    {
        // Arrange
        using var handler = new ResponseHandler(HttpStatusCode.TooManyRequests, new RetryConditionHeaderValue(TimeSpan.FromSeconds(30)));
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = CreateClient(httpClient);

        // Act
        var exception = await Should.ThrowAsync<RetryAfterHttpRequestException>(
            () => client.GetRunAsync(4201, TestContext.Current.CancellationToken));

        // Assert
        exception.RetryAfter.ShouldBe(TimeSpan.FromSeconds(30));
        exception.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
        exception.Message.ShouldBe("Response status code does not indicate success: 429 (Too Many Requests).");
    }

    [Fact]
    public async Task GetRunAsync_503WithRetryAfterDate_ThrowsWithTheTimeLeft()
    {
        // Arrange
        using var handler = new ResponseHandler(
            HttpStatusCode.ServiceUnavailable,
            new RetryConditionHeaderValue(DateTimeOffset.UtcNow.AddMinutes(2)));
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = CreateClient(httpClient);

        // Act
        var exception = await Should.ThrowAsync<RetryAfterHttpRequestException>(
            () => client.GetRunAsync(4201, TestContext.Current.CancellationToken));

        // Assert
        exception.RetryAfter.ShouldBeInRange(TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(2));
        exception.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task GetRunAsync_RetryAfterDateInThePast_ThrowsWithZero()
    {
        // Arrange
        using var handler = new ResponseHandler(
            HttpStatusCode.ServiceUnavailable,
            new RetryConditionHeaderValue(DateTimeOffset.UtcNow.AddMinutes(-1)));
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = CreateClient(httpClient);

        // Act
        var exception = await Should.ThrowAsync<RetryAfterHttpRequestException>(
            () => client.GetRunAsync(4201, TestContext.Current.CancellationToken));

        // Assert
        exception.RetryAfter.ShouldBe(TimeSpan.Zero);
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.NotFound)]
    public async Task GetRunAsync_FailureWithoutRetryAfter_ThrowsPlainHttpRequestException(HttpStatusCode status)
    {
        // Arrange
        using var handler = new ResponseHandler(status, null);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = CreateClient(httpClient);

        // Act
        var exception = await Should.ThrowAsync<HttpRequestException>(
            () => client.GetRunAsync(4201, TestContext.Current.CancellationToken));

        // Assert
        exception.GetType().ShouldBe(typeof(HttpRequestException));
        exception.StatusCode.ShouldBe(status);
    }

    private static AzureDevOpsBuildClient CreateClient(HttpClient httpClient)
    {
        return new AzureDevOpsBuildClient(
            httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);
    }

    private sealed class ResponseHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _status;
        private readonly RetryConditionHeaderValue? _retryAfter;

        public ResponseHandler(HttpStatusCode status, RetryConditionHeaderValue? retryAfter)
        {
            _status = status;
            _retryAfter = retryAfter;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(_status);
            response.Headers.RetryAfter = _retryAfter;
            return Task.FromResult(response);
        }
    }
}
