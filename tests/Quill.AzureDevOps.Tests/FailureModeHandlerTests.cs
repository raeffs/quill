using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Quill.Core;
using Quill.Tests.Shared;
using Shouldly;

namespace Quill.AzureDevOps.Tests;

public class FailureModeHandlerTests
{
    private static readonly Uri Url = new("https://devops.contoso.test/items/7");

    [Theory]
    [InlineData(400, typeof(RejectedException))]
    [InlineData(404, typeof(RejectedException))]
    [InlineData(409, typeof(RejectedException))]
    [InlineData(422, typeof(RejectedException))]
    [InlineData(412, typeof(RejectedException))]
    [InlineData(401, typeof(DeniedException))]
    [InlineData(403, typeof(DeniedException))]
    [InlineData(408, typeof(UnavailableException))]
    [InlineData(429, typeof(UnavailableException))]
    [InlineData(500, typeof(UnavailableException))]
    [InlineData(503, typeof(UnavailableException))]
    [InlineData(302, typeof(DefectException))]
    [InlineData(418, typeof(DefectException))]
    public async Task SendAsync_FailedStatus_ThrowsItsMode(int status, Type expected)
    {
        // Arrange
        using var inner = new ScriptedHandler(_ => Respond((HttpStatusCode)status));
        using var client = TestHttp.Client(inner);

        // Act
        var exception = await Should.ThrowAsync<QuillException>(() => client.GetAsync(Url, TestContext.Current.CancellationToken));

        // Assert
        exception.GetType().ShouldBe(expected);
    }

    [Theory]
    [InlineData(200)]
    [InlineData(204)]
    public async Task SendAsync_Success_ReturnsTheResponse(int status)
    {
        // Arrange
        using var inner = new ScriptedHandler(_ => Respond((HttpStatusCode)status, "{}"));
        using var client = TestHttp.Client(inner);

        // Act
        using var response = await client.GetAsync(Url, TestContext.Current.CancellationToken);

        // Assert
        ((int)response.StatusCode).ShouldBe(status);
    }

    [Fact]
    public async Task SendAsync_ServerMessage_IsTheTextWithTheStatus()
    {
        // Arrange
        using var inner = new ScriptedHandler(_ => Respond(HttpStatusCode.NotFound, """{"message":"TF401232: Work item 7 does not exist."}"""));
        using var client = TestHttp.Client(inner);

        // Act
        var exception = await Should.ThrowAsync<RejectedException>(() => client.GetAsync(Url, TestContext.Current.CancellationToken));

        // Assert
        exception.Message.ShouldBe("TF401232: Work item 7 does not exist. (404)");
    }

    [Fact]
    public async Task SendAsync_NoServerMessage_NamesTheStatus()
    {
        // Arrange
        using var inner = new ScriptedHandler(_ => Respond(HttpStatusCode.Unauthorized));
        using var client = TestHttp.Client(inner);

        // Act
        var exception = await Should.ThrowAsync<DeniedException>(() => client.GetAsync(Url, TestContext.Current.CancellationToken));

        // Assert
        exception.Message.ShouldBe("The server answered 401 (Unauthorized).");
    }

    [Fact]
    public async Task SendAsync_HtmlBodyOn502_IsUnavailableWithTheStatus()
    {
        // Arrange
        using var inner = new ScriptedHandler(_ => Respond(HttpStatusCode.BadGateway, "<html>Bad Gateway</html>"));
        using var client = TestHttp.Client(inner);

        // Act
        var exception = await Should.ThrowAsync<UnavailableException>(() => client.GetAsync(Url, TestContext.Current.CancellationToken));

        // Assert
        exception.Message.ShouldBe("The server answered 502 (Bad Gateway).");
    }

    [Fact]
    public async Task SendAsync_RetryAfterHeader_IsOnTheException()
    {
        // Arrange
        using var inner = new ScriptedHandler(_ =>
        {
            var response = Respond(HttpStatusCode.TooManyRequests);
            response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(7));
            return response;
        });
        using var client = TestHttp.Client(inner);

        // Act
        var exception = await Should.ThrowAsync<UnavailableException>(() => client.GetAsync(Url, TestContext.Current.CancellationToken));

        // Assert
        exception.RetryAfter.ShouldBe(TimeSpan.FromSeconds(7));
    }

    [Fact]
    public async Task SendAsync_NoRetryAfterHeader_LeavesItNull()
    {
        // Arrange
        using var inner = new ScriptedHandler(_ => Respond(HttpStatusCode.ServiceUnavailable));
        using var client = TestHttp.Client(inner);

        // Act
        var exception = await Should.ThrowAsync<UnavailableException>(() => client.GetAsync(Url, TestContext.Current.CancellationToken));

        // Assert
        exception.RetryAfter.ShouldBeNull();
    }

    [Fact]
    public async Task SendAsync_NetworkFailure_IsUnavailable()
    {
        // Arrange
        var failure = new HttpRequestException("refused");
        using var inner = new ScriptedHandler(_ => throw failure);
        using var client = TestHttp.Client(inner);

        // Act
        var exception = await Should.ThrowAsync<UnavailableException>(() => client.GetAsync(Url, TestContext.Current.CancellationToken));

        // Assert
        exception.Message.ShouldBe("No answer from the server: refused");
        exception.InnerException.ShouldBeSameAs(failure);
    }

    [Fact]
    public async Task SendAsync_NoAnswerInTime_IsUnavailable()
    {
        // Arrange
        using var inner = new WaitingHandler();
        using var client = TestHttp.Client(inner, TimeSpan.FromMilliseconds(50));

        // Act
        var exception = await Should.ThrowAsync<UnavailableException>(() => client.GetAsync(Url, TestContext.Current.CancellationToken));

        // Assert
        exception.Message.ShouldBe("The server did not answer within 0.05 seconds.");
    }

    [Fact]
    public async Task SendAsync_BodyStalls_IsUnavailable()
    {
        // Arrange
        using var inner = new ScriptedHandler(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ScriptedContent(WaitForever) });
        using var client = TestHttp.Client(inner, TimeSpan.FromMilliseconds(50));

        // Act
        var exception = await Should.ThrowAsync<UnavailableException>(() => client.GetAsync(Url, TestContext.Current.CancellationToken));

        // Assert
        exception.Message.ShouldBe("The server did not answer within 0.05 seconds.");
    }

    [Fact]
    public async Task SendAsync_CallerCancels_PassesTheCancellation()
    {
        // Arrange
        using var inner = new WaitingHandler();
        using var client = TestHttp.Client(inner);
        using var cancellation = new CancellationTokenSource();
        var call = client.GetAsync(Url, cancellation.Token);
        await cancellation.CancelAsync();

        // Act
        var exception = await Should.ThrowAsync<OperationCanceledException>(() => call);

        // Assert
        exception.ShouldNotBeAssignableTo<QuillException>();
    }

    [Fact]
    public async Task SendAsync_RejectedWhenMatches_OverridesTheTable()
    {
        // Arrange
        using var inner = new ScriptedHandler(_ => Respond(
            HttpStatusCode.InternalServerError,
            """{"message":"Run 4202 cannot change state.","typeKey":"InvalidBuildStateException"}"""));
        using var client = TestHttp.Client(inner);
        using var request = RequestRejectingInvalidBuildState();

        // Act
        var exception = await Should.ThrowAsync<RejectedException>(() => client.SendAsync(request, TestContext.Current.CancellationToken));

        // Assert
        exception.Message.ShouldBe("Run 4202 cannot change state. (500)");
    }

    [Fact]
    public async Task SendAsync_NetworkFailureDuringTheBody_IsUnavailable()
    {
        // Arrange
        using var inner = new ScriptedHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ScriptedContent(_ => throw new HttpRequestException("reset")),
        });
        using var client = TestHttp.Client(inner);

        // Act
        var exception = await Should.ThrowAsync<UnavailableException>(() => client.GetAsync(Url, TestContext.Current.CancellationToken));

        // Assert
        exception.Message.ShouldBe("No answer from the server: reset");
    }

    [Fact]
    public async Task SendAsync_CallerCancelsDuringTheBody_PassesTheCancellation()
    {
        // Arrange
        using var inner = new ScriptedHandler(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ScriptedContent(WaitForever) });
        using var client = TestHttp.Client(inner);
        using var cancellation = new CancellationTokenSource();
        var call = client.GetAsync(Url, cancellation.Token);
        await cancellation.CancelAsync();

        // Act
        var exception = await Should.ThrowAsync<OperationCanceledException>(() => call);

        // Assert
        exception.ShouldNotBeAssignableTo<QuillException>();
    }

    [Fact]
    public async Task SendAsync_NoReasonPhrase_NamesTheStatusAlone()
    {
        // Arrange
        using var inner = new ScriptedHandler(_ =>
        {
            var response = Respond((HttpStatusCode)418);
            response.ReasonPhrase = null;
            return response;
        });
        using var client = TestHttp.Client(inner);

        // Act
        var exception = await Should.ThrowAsync<DefectException>(() => client.GetAsync(Url, TestContext.Current.CancellationToken));

        // Assert
        exception.Message.ShouldBe("The server answered 418.");
    }

    [Fact]
    public async Task SendAsync_CallerCancelsAndTransportThrows_PassesTheCancellation()
    {
        // Arrange
        using var inner = new ScriptedHandler(_ => throw new HttpRequestException("aborted"));
        using var client = TestHttp.Client(inner);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        // Act
        var exception = await Should.ThrowAsync<OperationCanceledException>(() => client.GetAsync(Url, cancellation.Token));

        // Assert
        exception.ShouldNotBeAssignableTo<QuillException>();
    }

    [Fact]
    public async Task SendAsync_RejectedWhenDoesNotMatch_KeepsTheTable()
    {
        // Arrange
        using var inner = new ScriptedHandler(_ => Respond(HttpStatusCode.InternalServerError, """{"typeKey":"SomethingElseException"}"""));
        using var client = TestHttp.Client(inner);
        using var request = RequestRejectingInvalidBuildState();

        // Act and assert
        await Should.ThrowAsync<UnavailableException>(() => client.SendAsync(request, TestContext.Current.CancellationToken));
    }

    private static HttpRequestMessage RequestRejectingInvalidBuildState()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, Url);
        request.Options.Set(FailureModeHandler.RejectedWhen, e => e.Status == 500 && string.Equals(e.TypeKey, "InvalidBuildStateException", StringComparison.Ordinal));
        return request;
    }

    private static async Task WaitForever(CancellationToken cancellationToken)
    {
        await Task.Delay(Timeout.Infinite, cancellationToken);
    }

    private static HttpResponseMessage Respond(HttpStatusCode status, string body = "")
    {
        return new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
    }

    private sealed class ScriptedHandler(Func<HttpRequestMessage, HttpResponseMessage> script) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(script(request));
        }
    }

    private sealed class WaitingHandler : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
            throw new InvalidOperationException("Unreachable.");
        }
    }

    private sealed class ScriptedContent(Func<CancellationToken, Task> serialize) : HttpContent
    {
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
        {
            return serialize(CancellationToken.None);
        }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context, CancellationToken cancellationToken)
        {
            return serialize(cancellationToken);
        }

        protected override bool TryComputeLength(out long length)
        {
            length = -1;
            return false;
        }
    }
}
