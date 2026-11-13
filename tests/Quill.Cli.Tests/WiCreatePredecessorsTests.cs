using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Quill.AzureDevOps;
using Quill.Core;
using Quill.Core.Markdown;
using Quill.Core.Models;
using Quill.Tests.Shared;
using Shouldly;

namespace Quill.Cli.Tests;

[Collection(nameof(ProcessStateCollection))]
public sealed class WiCreatePredecessorsTests : IDisposable
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
    [InlineData("'0'", "--predecessor", "0")]
    [InlineData("'10'", "--predecessor", "10", "--predecessor", "10")]
    public async Task InvokeAsync_InvalidPredecessor_Returns3BeforeAnyRequest(string message, params string[] flags)
    {
        using var server = new ScriptedServer();

        (await RunAsync(server, flags)).ShouldBe(3);

        server.RequestCount.ShouldBe(0);
        _output.ToString().ShouldContain(message);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("1.5")]
    [InlineData("-1")]
    [InlineData("99999999999")]
    public async Task InvokeAsync_UnparsableOrNegativePredecessor_Returns3BeforeAnyRequest(string value)
    {
        using var server = new ScriptedServer();

        (await RunAsync(server, "--predecessor", value)).ShouldBe(3);

        server.RequestCount.ShouldBe(0);
    }

    [Fact]
    public async Task InvokeAsync_InvalidPredecessor_WithMissingDescriptionFile_ReportsThePredecessor()
    {
        using var server = new ScriptedServer();

        (await RunAsync(server, "--predecessor", "0", "--description-file", "/no/such/file.md")).ShouldBe(3);

        _output.ToString().ShouldContain("'0'");
    }

    [Fact]
    public async Task InvokeAsync_Predecessors_ReachTheServerAndTheOutput()
    {
        using var server = new ScriptedServer();

        (await RunAsync(server, "--predecessor", "10", "--predecessor", "11")).ShouldBe(0);

        var body = server.CreateBody.ShouldNotBeNull();
        body.ShouldContain("/relations/-");
        body.ShouldContain("System.LinkTypes.Dependency-Reverse");
        using var output = JsonDocument.Parse(_output.ToString());
        output.RootElement.GetProperty("predecessors").EnumerateArray().Select(p => p.GetInt32()).ShouldBe([10, 11]);
    }

    [Fact]
    public async Task InvokeAsync_ServerRejects_Returns1WithServerMessage()
    {
        using var server = new ScriptedServer { RejectPatches = true };

        (await RunAsync(server, "--predecessor", "999999")).ShouldBe(1);

        _output.ToString().ShouldContain("TF401232");
    }

    private async Task<int> RunAsync(ScriptedServer server, params string[] flags)
    {
#pragma warning disable CA2000 // The service provider disposes the client that holds it
        var httpClient = TestHttp.Client(server);
#pragma warning restore CA2000
        await using var services = new ServiceCollection()
            .AddLogging()
            .AddSingleton(new QuillConfig { ServerUrl = ServerUrl, Collection = Collection, Project = Project })
            .AddSingleton(httpClient)
            .AddSingleton(new AzureDevOpsClient(httpClient, ServerUrl, Collection, Project))
            .AddSingleton<IAzureDevOpsClient>(sp => sp.GetRequiredService<AzureDevOpsClient>())
            .AddSingleton<IWorkItemBatchFetcher>(sp => sp.GetRequiredService<AzureDevOpsClient>())
            .AddSingleton<RichText>()
            .AddSingleton(new AzureDevOpsIdentityClient(httpClient, ServerUrl, Collection))
            .AddSingleton<IIdentityClient>(sp => sp.GetRequiredService<AzureDevOpsIdentityClient>())
            .BuildServiceProvider();
        Console.SetOut(_output);

        return await CliHost.InvokeAsync(
            ["wi", "create", "--type", "Task", "--title", "t", "--parent", "42", .. flags], services);
    }

    private sealed class ScriptedServer : HttpMessageHandler
    {
        public string? CreateBody { get; private set; }

        public int RequestCount { get; private set; }

        public bool RejectPatches { get; init; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestCount++;

            if (request.Method == HttpMethod.Patch)
            {
                CreateBody = await request.Content!.ReadAsStringAsync(cancellationToken);
                return RejectPatches
                    ? Json("""{"message": "TF401232: cycle"}""", HttpStatusCode.BadRequest)
                    : Json("""{"id": 1000, "fields": {}}""");
            }

            return request.RequestUri!.AbsolutePath.EndsWith("/1000", StringComparison.Ordinal)
                ? Json("""
                    {"id": 1000, "fields": {"System.WorkItemType": "Task", "System.Title": "t", "System.State": "New"}, "relations": [
                        {"rel": "System.LinkTypes.Dependency-Reverse", "url": "https://devops.example.com/col/_apis/wit/workitems/10"},
                        {"rel": "System.LinkTypes.Dependency-Reverse", "url": "https://devops.example.com/col/_apis/wit/workitems/11"}
                    ]}
                    """)
                : Json("""{"id": 42, "fields": {"System.WorkItemType": "Product Backlog Item", "System.Title": "p", "System.State": "New"}}""");
        }

        private static HttpResponseMessage Json(string body, HttpStatusCode status = HttpStatusCode.OK) =>
            new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
    }
}
