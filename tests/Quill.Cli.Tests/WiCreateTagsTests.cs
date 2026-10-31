using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Quill.AzureDevOps;
using Quill.Core.Models;
using Shouldly;

namespace Quill.Cli.Tests;

[Collection(nameof(ProcessStateCollection))]
public sealed class WiCreateTagsTests : IDisposable
{
    private const string ServerUrl = "https://devops.example.com";
    private const string Collection = "col";
    private const string Project = "proj";

#pragma warning disable CA2213 // The process owns the original Console.Out; this class only puts it back
    private readonly TextWriter _originalOut = Console.Out;
#pragma warning restore CA2213
    private readonly StringWriter _output = new();

    public void Dispose()
    {
        Console.SetOut(_originalOut);
        Environment.ExitCode = 0;
        _output.Dispose();
    }

    [Theory]
    [InlineData("a;b", "must not contain")]
    [InlineData("a,b", "must not contain")]
    [InlineData(" ", "must not be empty")]
    [InlineData("", "must not be empty")]
    public async Task InvokeAsync_InvalidTag_Returns3BeforeAnyRequest(string tag, string message)
    {
        using var services = new ServiceCollection().AddLogging().AddSingleton(new QuillConfig()).BuildServiceProvider();
        Console.SetOut(_output);

        var exitCode = await CliHost.InvokeAsync(
            ["wi", "create", "--type", "Task", "--title", "t", "--parent", "42", "--tag", tag], services);

        // The message matters: the unregistered client also ends in exit 3, so
        // the code alone would pass even if the tags were never looked at.
        exitCode.ShouldBe(3);
        _output.ToString().ShouldContain(message);
    }

    [Fact]
    public async Task InvokeAsync_InvalidTag_WithMissingDescriptionFile_ReportsTheTag()
    {
        using var services = new ServiceCollection().AddLogging().AddSingleton(new QuillConfig()).BuildServiceProvider();
        Console.SetOut(_output);

        var exitCode = await CliHost.InvokeAsync(
            ["wi", "create", "--type", "Task", "--title", "t", "--parent", "42", "--tag", "a;b", "--description-file", "/no/such/file.md"],
            services);

        exitCode.ShouldBe(3);
        _output.ToString().ShouldContain("must not contain");
    }

    [Fact]
    public async Task InvokeAsync_Tags_ReachTheServerAndTheOutput()
    {
        using var handler = new ScriptedServer();
#pragma warning disable CA2000 // The service provider disposes the client that holds it
        var httpClient = new HttpClient(handler);
#pragma warning restore CA2000
        using var services = new ServiceCollection()
            .AddLogging()
            .AddSingleton(new QuillConfig { ServerUrl = ServerUrl, Collection = Collection, Project = Project })
            .AddSingleton(httpClient)
            .AddSingleton(new AzureDevOpsClient(httpClient, ServerUrl, Collection, Project))
            .BuildServiceProvider();
        Console.SetOut(_output);

        var exitCode = await CliHost.InvokeAsync(
            ["wi", "create", "--type", "Task", "--title", "t", "--parent", "42", "--tag", " a ", "--tag", "b", "--tag", "a"],
            services);

        exitCode.ShouldBe(0);
        var body = handler.CreateBody.ShouldNotBeNull();
        body.ShouldContain("/fields/System.Tags");
        body.ShouldContain("a; b");
        using var output = JsonDocument.Parse(_output.ToString());
        output.RootElement.GetProperty("tags").EnumerateArray().Select(t => t.GetString()).ShouldBe(["a", "b"]);
    }

    private sealed class ScriptedServer : HttpMessageHandler
    {
        public string? CreateBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.Method == HttpMethod.Patch)
            {
                CreateBody = await request.Content!.ReadAsStringAsync(cancellationToken);
                return Json("""{"id": 1000, "fields": {}}""");
            }

            return request.RequestUri!.AbsolutePath.EndsWith("/1000", StringComparison.Ordinal)
                ? Json("""{"id": 1000, "fields": {"System.WorkItemType": "Task", "System.Title": "t", "System.State": "New", "System.Tags": "a; b"}}""")
                : Json("""{"id": 42, "fields": {"System.WorkItemType": "Product Backlog Item", "System.Title": "p", "System.State": "New"}}""");
        }

        private static HttpResponseMessage Json(string body) =>
            new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
    }
}
