using System.Net;
using System.Text.Json;
using Quill.AzureDevOps.Dto;
using Quill.Core;
using Quill.Tests.Shared;
using Shouldly;

namespace Quill.AzureDevOps.Tests;

public class AzureDevOpsServerTests
{
    private const string WorkItemPath = "_apis/wit/workitems/12";

    public static TheoryData<HttpStatusCode, string> EmptyAnswers => new()
    {
        { HttpStatusCode.NoContent, string.Empty },
        { HttpStatusCode.OK, string.Empty },
        { HttpStatusCode.OK, "   " },
    };

    public static TheoryData<HttpStatusCode, string> EmptyAnswersForRead => new()
    {
        { HttpStatusCode.NoContent, string.Empty },
        { HttpStatusCode.OK, string.Empty },
        { HttpStatusCode.OK, "   " },
        { HttpStatusCode.OK, "null" },
    };

    [Fact]
    public async Task ReadAsync_ProjectScope_SendsBaseProjectPathAndVersion()
    {
        using var handler = new FakeHttpHandler("""{"id":12}""", HttpStatusCode.OK);
        using var httpClient = TestHttp.Client(handler);
        var server = TestHttp.Server(httpClient);

        var workItem = await server.ReadAsync(
            ServerRequest.Get(ServerScope.Project, WorkItemPath),
            ServerTestJsonContext.Default.WorkItemResponse,
            TestContext.Current.CancellationToken);

        handler.LastRequest!.RequestUri!.ToString().ShouldBe("https://myserver.com/tfs/DefaultCollection/MyProject/_apis/wit/workitems/12?api-version=7.1-preview");
        workItem.Id.ShouldBe(12);
    }

    [Fact]
    public async Task ReadAsync_CollectionScope_LeavesTheProjectOut()
    {
        using var handler = new FakeHttpHandler("""{"id":12}""", HttpStatusCode.OK);
        using var httpClient = TestHttp.Client(handler);
        var server = TestHttp.Server(httpClient);

        await server.ReadAsync(
            ServerRequest.Get(ServerScope.Collection, "_apis/connectionData"),
            ServerTestJsonContext.Default.WorkItemResponse,
            TestContext.Current.CancellationToken);

        handler.LastRequest!.RequestUri!.ToString().ShouldStartWith("https://myserver.com/tfs/DefaultCollection/_apis/connectionData");
    }

    [Fact]
    public async Task ReadAsync_PathWithAQuery_AppendsTheVersionWithAnAmpersand()
    {
        using var handler = new FakeHttpHandler("""{"id":12}""", HttpStatusCode.OK);
        using var httpClient = TestHttp.Client(handler);
        var server = TestHttp.Server(httpClient);

        await server.ReadAsync(
            ServerRequest.Get(ServerScope.Project, "_apis/wit/workitems/12?$expand=relations"),
            ServerTestJsonContext.Default.WorkItemResponse,
            TestContext.Current.CancellationToken);

        handler.LastRequest!.RequestUri!.ToString().ShouldEndWith("?$expand=relations&api-version=7.1-preview");
    }

    [Fact]
    public async Task ReadAsync_VersionOverride_SendsThatVersion()
    {
        using var handler = new FakeHttpHandler("""{"id":12}""", HttpStatusCode.OK);
        using var httpClient = TestHttp.Client(handler);
        var server = TestHttp.Server(httpClient);

        await server.ReadAsync(
            ServerRequest.Get(ServerScope.Project, WorkItemPath) with { ApiVersion = "7.1" },
            ServerTestJsonContext.Default.WorkItemResponse,
            TestContext.Current.CancellationToken);

        handler.LastRequest!.RequestUri!.ToString().ShouldEndWith("api-version=7.1");
    }

    [Fact]
    public async Task ReadAsync_NoVersion_SendsNone()
    {
        using var handler = new FakeHttpHandler("""{"id":12}""", HttpStatusCode.OK);
        using var httpClient = TestHttp.Client(handler);
        var server = TestHttp.Server(httpClient);

        await server.ReadAsync(
            ServerRequest.Get(ServerScope.Project, WorkItemPath) with { ApiVersion = null },
            ServerTestJsonContext.Default.WorkItemResponse,
            TestContext.Current.CancellationToken);

        handler.LastRequest!.RequestUri!.ToString().ShouldNotContain("api-version");
    }

    [Fact]
    public async Task ReadAsync_ServerUrlWithTrailingSlash_SendsOneSlash()
    {
        using var handler = new FakeHttpHandler("""{"id":12}""", HttpStatusCode.OK);
        using var httpClient = TestHttp.Client(handler);
        var server = TestHttp.Server(httpClient, serverUrl: "https://myserver.com/tfs/");

        await server.ReadAsync(
            ServerRequest.Get(ServerScope.Project, WorkItemPath),
            ServerTestJsonContext.Default.WorkItemResponse,
            TestContext.Current.CancellationToken);

        handler.LastRequest!.RequestUri!.ToString().ShouldStartWith("https://myserver.com/tfs/DefaultCollection/");
    }

    [Fact]
    public async Task ReadAsync_AnyRequest_AcceptsJson()
    {
        using var handler = new FakeHttpHandler("""{"id":12}""", HttpStatusCode.OK);
        using var httpClient = TestHttp.Client(handler);
        var server = TestHttp.Server(httpClient);

        await server.ReadAsync(
            ServerRequest.Get(ServerScope.Project, WorkItemPath),
            ServerTestJsonContext.Default.WorkItemResponse,
            TestContext.Current.CancellationToken);

        handler.LastRequest!.Headers.Accept.ToString().ShouldContain("application/json");
    }

    [Fact]
    public async Task ReadAsync_BodyIsNotJson_ThrowsADefectThatNamesTheRequest()
    {
        using var handler = new FakeHttpHandler("<html>Sign in</html>", HttpStatusCode.OK);
        using var httpClient = TestHttp.Client(handler);
        var server = TestHttp.Server(httpClient);

        var failure = await Should.ThrowAsync<DefectException>(() => server.ReadAsync(
            ServerRequest.Get(ServerScope.Project, WorkItemPath),
            ServerTestJsonContext.Default.WorkItemResponse,
            TestContext.Current.CancellationToken));

        failure.Message.ShouldBe("Cannot read the answer to GET _apis/wit/workitems/12: the body is not valid JSON.");
        failure.InnerException.ShouldBeOfType<JsonException>();
    }

    [Theory]
    [MemberData(nameof(EmptyAnswersForRead))]
    public async Task ReadAsync_EmptyBody_ThrowsADefect(HttpStatusCode status, string body)
    {
        using var handler = new FakeHttpHandler(body, status);
        using var httpClient = TestHttp.Client(handler);
        var server = TestHttp.Server(httpClient);

        var failure = await Should.ThrowAsync<DefectException>(() => server.ReadAsync(
            ServerRequest.Get(ServerScope.Project, WorkItemPath),
            ServerTestJsonContext.Default.WorkItemResponse,
            TestContext.Current.CancellationToken));

        failure.Message.ShouldBe("Cannot read the answer to GET _apis/wit/workitems/12: the body is empty.");
    }

    [Fact]
    public async Task ReadAsync_RequiredValueMissing_ThrowsADefectThatNamesTheValue()
    {
        using var handler = new FakeHttpHandler("{}", HttpStatusCode.OK);
        using var httpClient = TestHttp.Client(handler);
        var server = TestHttp.Server(httpClient);

        var failure = await Should.ThrowAsync<DefectException>(() => server.ReadAsync(
            ServerRequest.Get(ServerScope.Project, WorkItemPath),
            ServerTestJsonContext.Default.WiqlQueryRequest,
            TestContext.Current.CancellationToken));

        failure.Message.ShouldStartWith("Cannot read the answer to GET ");
        failure.Message.ShouldContain("'query'");
        failure.InnerException.ShouldBeOfType<JsonException>();
    }

    [Fact]
    public async Task ReadAsync_RequiredValueHasTheWrongType_ThrowsADefect()
    {
        using var handler = new FakeHttpHandler("""{"id":"twelve"}""", HttpStatusCode.OK);
        using var httpClient = TestHttp.Client(handler);
        var server = TestHttp.Server(httpClient);

        var failure = await Should.ThrowAsync<DefectException>(() => server.ReadAsync(
            ServerRequest.Get(ServerScope.Project, WorkItemPath),
            ServerTestJsonContext.Default.WorkItemResponse,
            TestContext.Current.CancellationToken));

        failure.Message.ShouldStartWith("Cannot read the answer to GET ");
        failure.Message.ShouldNotEndWith("the body is not valid JSON.");
        failure.InnerException.ShouldBeOfType<JsonException>();
    }

    [Fact]
    public async Task ReadAsync_CancelledToken_ThrowsOperationCanceled()
    {
        using var handler = new FakeHttpHandler("""{"id":12}""", HttpStatusCode.OK);
        using var httpClient = TestHttp.Client(handler);
        var server = TestHttp.Server(httpClient);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(() => server.ReadAsync(
            ServerRequest.Get(ServerScope.Project, WorkItemPath),
            ServerTestJsonContext.Default.WorkItemResponse,
            cancellation.Token));
    }

    [Fact]
    public async Task ReadAsync_PathWithAQuery_LeavesTheQueryOutOfTheMessage()
    {
        using var handler = new FakeHttpHandler("<html>", HttpStatusCode.OK);
        using var httpClient = TestHttp.Client(handler);
        var server = TestHttp.Server(httpClient);

        var failure = await Should.ThrowAsync<DefectException>(() => server.ReadAsync(
            ServerRequest.Get(ServerScope.Project, "_apis/identities?filterValue=Jane%20Doe"),
            ServerTestJsonContext.Default.WorkItemResponse,
            TestContext.Current.CancellationToken));

        failure.Message.ShouldBe("Cannot read the answer to GET _apis/identities: the body is not valid JSON.");
    }

    [Theory]
    [MemberData(nameof(EmptyAnswers))]
    public async Task ReadOrNothingAsync_EmptyBody_ReturnsNull(HttpStatusCode status, string body)
    {
        using var handler = new FakeHttpHandler(body, status);
        using var httpClient = TestHttp.Client(handler);
        var server = TestHttp.Server(httpClient);

        var answer = await server.ReadOrNothingAsync(
            ServerRequest.Get(ServerScope.Project, WorkItemPath),
            ServerTestJsonContext.Default.WorkItemResponse,
            TestContext.Current.CancellationToken);

        answer.ShouldBeNull();
    }

    [Fact]
    public async Task ReadOrNothingAsync_BodyIsNotJson_ThrowsADefect()
    {
        using var handler = new FakeHttpHandler("<html>", HttpStatusCode.OK);
        using var httpClient = TestHttp.Client(handler);
        var server = TestHttp.Server(httpClient);

        await Should.ThrowAsync<DefectException>(() => server.ReadOrNothingAsync(
            ServerRequest.Get(ServerScope.Project, WorkItemPath),
            ServerTestJsonContext.Default.WorkItemResponse,
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SendAsync_BodyIsNotJson_DoesNotThrow()
    {
        using var handler = new FakeHttpHandler("<html>", HttpStatusCode.OK);
        using var httpClient = TestHttp.Client(handler);
        var server = TestHttp.Server(httpClient);

        await server.SendAsync(
            ServerRequest.Get(ServerScope.Project, WorkItemPath),
            TestContext.Current.CancellationToken);

        handler.RequestCount.ShouldBe(1);
    }

    [Fact]
    public async Task ReadAsync_Post_SendsTheBodyAsJson()
    {
        using var handler = new FakeHttpHandler("""{"id":12}""", HttpStatusCode.OK);
        using var httpClient = TestHttp.Client(handler);
        var server = TestHttp.Server(httpClient);
        var body = new WiqlQueryRequest { Query = "SELECT [System.Id] FROM WorkItems" };

        await server.ReadAsync(
            ServerRequest.Post(ServerScope.Project, "_apis/wit/wiql", body, ServerTestJsonContext.Default.WiqlQueryRequest),
            ServerTestJsonContext.Default.WorkItemResponse,
            TestContext.Current.CancellationToken);

        handler.LastRequest!.Method.ShouldBe(HttpMethod.Post);
        handler.LastRequest.Content!.Headers.ContentType!.ToString().ShouldBe("application/json; charset=utf-8");
        handler.LastRequestBody.ShouldBe(JsonSerializer.Serialize(body, ServerTestJsonContext.Default.WiqlQueryRequest));
    }

    [Fact]
    public async Task SendAsync_JsonPatch_SendsThePatchContentType()
    {
        using var handler = new FakeHttpHandler("{}", HttpStatusCode.OK);
        using var httpClient = TestHttp.Client(handler);
        var server = TestHttp.Server(httpClient);
        var operations = new List<JsonPatchOperation> { new() { Op = "remove", Path = "/fields/System.Title" } };

        await server.SendAsync(
            ServerRequest.JsonPatch(HttpMethod.Patch, ServerScope.Project, WorkItemPath, operations),
            TestContext.Current.CancellationToken);

        handler.LastRequest!.Method.ShouldBe(HttpMethod.Patch);
        handler.LastRequest.Content!.Headers.ContentType!.ToString().ShouldBe("application/json-patch+json; charset=utf-8");
    }

    [Fact]
    public async Task SendAsync_RejectedWhenMatches_ThrowsRejected()
    {
        using var handler = new FakeHttpHandler("{}", HttpStatusCode.InternalServerError);
        using var httpClient = TestHttp.Client(handler);
        var server = TestHttp.Server(httpClient);

        await Should.ThrowAsync<RejectedException>(() => server.SendAsync(
            ServerRequest.Get(ServerScope.Project, WorkItemPath) with { RejectedWhen = _ => true },
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SendAsync_Status503_ThrowsUnavailable()
    {
        using var handler = new FakeHttpHandler("{}", HttpStatusCode.ServiceUnavailable);
        using var httpClient = TestHttp.Client(handler);
        var server = TestHttp.Server(httpClient);

        await Should.ThrowAsync<UnavailableException>(() => server.SendAsync(
            ServerRequest.Get(ServerScope.Project, WorkItemPath),
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public void UrlOf_CollectionScope_GivesTheUrlWithoutAVersion()
    {
        using var handler = new FakeHttpHandler("{}", HttpStatusCode.OK);
        using var httpClient = TestHttp.Client(handler);
        var server = TestHttp.Server(httpClient);

        server.UrlOf(ServerScope.Collection, "_apis/wit/workitems/7").ShouldBe("https://myserver.com/tfs/DefaultCollection/_apis/wit/workitems/7");
    }
}
