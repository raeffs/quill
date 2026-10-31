using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Quill.AzureDevOps;
using Quill.Core.Markdown;
using Quill.Core.Models;
using Shouldly;

namespace Quill.Cli.Tests;

[Collection(nameof(ProcessStateCollection))]
public sealed class BacklogPredecessorsTests : IDisposable
{
    private const string ServerUrl = "https://devops.example.com";
    private const string Collection = "col";
    private const string Project = "proj";
    private const string TwoPredecessors = """
        [
            {"rel": "System.LinkTypes.Hierarchy-Reverse", "url": "https://devops.example.com/_apis/wit/workItems/42"},
            {"rel": "System.LinkTypes.Dependency-Reverse", "url": "https://devops.example.com/_apis/wit/workItems/2"},
            {"rel": "System.LinkTypes.Dependency-Reverse", "url": "https://devops.example.com/_apis/wit/workItems/3"}
        ]
        """;

#pragma warning disable CA2213 // The process owns the original Console.Out; this class only puts it back
    private readonly TextWriter _originalOut = Console.Out;
#pragma warning restore CA2213
    private readonly StringWriter _output = new();
    private readonly string _path = Path.GetTempFileName();

    public void Dispose()
    {
        Console.SetOut(_originalOut);
        Environment.ExitCode = 0;
        File.Delete(_path);
        _output.Dispose();
    }

    [Fact]
    public async Task Pull_WritesPredecessors()
    {
        using var server = new ScriptedServer(TwoPredecessors);

        (await RunAsync(server, "pull", "1", _path)).ShouldBe(0);

        FrontmatterParser.Parse(await ReadFileAsync()).Predecessors.ShouldBe([2, 3]);
    }

    [Fact]
    public async Task Pull_NoLinks_WritesEmptyList()
    {
        using var server = new ScriptedServer("[]");

        (await RunAsync(server, "pull", "1", _path)).ShouldBe(0);

        (await ReadFileAsync()).ShouldContain("predecessors: []");
    }

    [Fact]
    public async Task Push_ExactSet_AddsAndRemovesInOnePatch()
    {
        using var server = new ScriptedServer(TwoPredecessors);
        await WriteFileAsync(1, "predecessors: [1, 2]");

        (await RunAsync(server, "push", _path)).ShouldBe(0);

        var patch = server.Patches.ShouldHaveSingleItem();
        patch.ShouldContain("/fields/System.Title");
        patch.ShouldContain("""{"op":"remove","path":"/relations/2"}""");
        patch.ShouldNotContain("/relations/1\"");
        patch.ShouldContain("Dependency-Reverse");
        patch.ShouldContain("workitems/1\"");
        using var output = JsonDocument.Parse(_output.ToString());
        output.RootElement.GetProperty("updatedFields").EnumerateArray().Select(f => f.GetString()).ShouldBe(["title", "description", "predecessors"]);
        output.RootElement.GetProperty("relationsAdded").GetArrayLength().ShouldBe(0);
    }

    [Fact]
    public async Task Push_NoKey_SendsNoLinkOperations()
    {
        using var server = new ScriptedServer(TwoPredecessors);
        await WriteFileAsync(1, string.Empty);

        (await RunAsync(server, "push", _path)).ShouldBe(0);

        server.Patches.ShouldHaveSingleItem().ShouldNotContain("/relations");
        _output.ToString().ShouldNotContain("predecessors");
    }

    [Fact]
    public async Task Push_EmptyList_RemovesAll()
    {
        using var server = new ScriptedServer(TwoPredecessors);
        await WriteFileAsync(1, "predecessors: []");

        (await RunAsync(server, "push", _path)).ShouldBe(0);

        var patch = server.Patches.ShouldHaveSingleItem();
        patch.ShouldContain("""{"op":"remove","path":"/relations/2"},{"op":"remove","path":"/relations/1"}""");
        patch.ShouldNotContain("/relations/0");
    }

    [Fact]
    public async Task Push_SameSet_OmitsPredecessorsFromUpdatedFields()
    {
        using var server = new ScriptedServer(TwoPredecessors);
        await WriteFileAsync(1, "predecessors: [3, 2]");

        (await RunAsync(server, "push", _path)).ShouldBe(0);

        var patch = server.Patches.ShouldHaveSingleItem();
        patch.ShouldNotContain("/relations");
        patch.ShouldNotContain("/rev");
        _output.ToString().ShouldNotContain("predecessors");
    }

    [Fact]
    public async Task Push_BodyLinkToPredecessor_AddsRelatedOnly()
    {
        using var server = new ScriptedServer(TwoPredecessors);
        await WriteFileAsync(1, "predecessors: [2, 3]", "[x](#2)");

        (await RunAsync(server, "push", _path)).ShouldBe(0);

        server.Patches.Count.ShouldBe(2);
        server.Patches[0].ShouldNotContain("/relations");
        server.Patches[1].ShouldContain("System.LinkTypes.Related");
        using var output = JsonDocument.Parse(_output.ToString());
        output.RootElement.GetProperty("relationsAdded").EnumerateArray().Select(r => r.GetInt32()).ShouldBe([2]);
    }

    [Fact]
    public async Task Create_WithPredecessors_SendsLinksAndRewritesFile()
    {
        using var server = new ScriptedServer("[]");
        await WriteFileAsync(0, "predecessors: [10, 11]");

        (await RunAsync(server, "create", _path)).ShouldBe(0);

        var patch = server.Patches.ShouldHaveSingleItem();
        patch.ShouldContain("Dependency-Reverse");
        patch.ShouldContain("workitems/10\"");
        patch.ShouldContain("workitems/11\"");
        var rewritten = FrontmatterParser.Parse(await ReadFileAsync());
        rewritten.Id.ShouldBe(1000);
        rewritten.Predecessors.ShouldBe([10, 11]);
        _output.ToString().Trim().ShouldBe("""{"id":1000,"title":"T"}""");
    }

    [Fact]
    public async Task Create_NoKey_RewritesEmptyList()
    {
        using var server = new ScriptedServer("[]");
        await WriteFileAsync(0, string.Empty);

        (await RunAsync(server, "create", _path)).ShouldBe(0);

        (await ReadFileAsync()).ShouldContain("predecessors: []");
    }

    [Theory]
    [InlineData("push", "[0]", "'0'")]
    [InlineData("push", "[1, 1]", "'1'")]
    [InlineData("push", "[abc]", "Invalid frontmatter")]
    [InlineData("create", "[0]", "'0'")]
    [InlineData("create", "[1, 1]", "'1'")]
    [InlineData("create", "[abc]", "Invalid frontmatter")]
    public async Task InvokeAsync_InvalidPredecessors_Returns3BeforeAnyRequest(string command, string value, string message)
    {
        using var server = new ScriptedServer("[]");
        await WriteFileAsync(0, $"predecessors: {value}");

        (await RunAsync(server, command, _path)).ShouldBe(3);

        server.RequestCount.ShouldBe(0);
        _output.ToString().ShouldContain(message);
    }

    [Theory]
    [InlineData("push", 1)]
    [InlineData("create", 0)]
    public async Task InvokeAsync_ServerRejects_Returns1WithServerMessage(string command, int id)
    {
        using var server = new ScriptedServer("[]") { RejectPatches = true };
        await WriteFileAsync(id, "predecessors: [10]");

        (await RunAsync(server, command, _path)).ShouldBe(1);

        _output.ToString().ShouldContain("TF401232");
        FrontmatterParser.Parse(await ReadFileAsync()).Id.ShouldBe(id);
    }

    private async Task WriteFileAsync(int id, string line, string body = "Body") =>
        await File.WriteAllTextAsync(_path, $"---\nid: {id}\ntype: Task\ntitle: T\nparentId: 42\n{line}\n---\n\n{body}\n", TestContext.Current.CancellationToken);

    private async Task<string> ReadFileAsync() =>
        await File.ReadAllTextAsync(_path, TestContext.Current.CancellationToken);

    private async Task<int> RunAsync(ScriptedServer server, params string[] args)
    {
#pragma warning disable CA2000 // The service provider disposes the client that holds it
        var httpClient = new HttpClient(server, disposeHandler: false);
#pragma warning restore CA2000
        await using var services = new ServiceCollection()
            .AddLogging()
            .AddSingleton(new QuillConfig { ServerUrl = ServerUrl, Collection = Collection, Project = Project, AllowedStates = ["New"] })
            .AddSingleton(httpClient)
            .AddSingleton(new AzureDevOpsClient(httpClient, ServerUrl, Collection, Project))
            .AddSingleton(new AzureDevOpsIdentityClient(httpClient, ServerUrl, Collection))
            .BuildServiceProvider();
        Console.SetOut(_output);

        return await CliHost.InvokeAsync(["backlog", .. args], services);
    }

    private sealed class ScriptedServer(string relations) : HttpMessageHandler
    {
        public List<string> Patches { get; } = [];

        public int RequestCount { get; private set; }

        public bool RejectPatches { get; init; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestCount++;

            if (request.Method == HttpMethod.Patch)
            {
                Patches.Add(await request.Content!.ReadAsStringAsync(cancellationToken));
                return RejectPatches
                    ? Json("""{"message": "TF401232: cycle"}""", HttpStatusCode.BadRequest)
                    : Json("""{"id": 1000, "fields": {}}""");
            }

            var path = request.RequestUri!.AbsolutePath;
            if (path.EndsWith("/connectionData", StringComparison.Ordinal))
            {
                return Json("""{"authenticatedUser": {"id": "u", "providerDisplayName": "U"}}""");
            }

            if (path.EndsWith("/42", StringComparison.Ordinal))
            {
                return Json("""{"id": 42, "fields": {"System.WorkItemType": "Product Backlog Item", "System.Title": "P", "System.State": "New", "System.AssignedTo": {"id": "u"}}}""");
            }

            var id = path[(path.LastIndexOf('/') + 1)..];
            return Json($$$"""{"id": {{{id}}}, "rev": 7, "fields": {"System.WorkItemType": "Task", "System.Title": "T", "System.State": "New", "System.AssignedTo": {"id": "u"}}, "relations": {{{relations}}} }""");
        }

        private static HttpResponseMessage Json(string body, HttpStatusCode status = HttpStatusCode.OK) =>
            new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
    }
}
