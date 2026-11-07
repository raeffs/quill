using Microsoft.Extensions.DependencyInjection;
using Quill.AzureDevOps;
using Quill.Cli.Commands.Backlog;
using Quill.Core;
using Quill.Core.Actions;
using Quill.Core.Markdown;
using Quill.Core.Models;
using Shouldly;

namespace Quill.Cli.Tests;

[CollectionDefinition(nameof(ProcessStateCollection), DisableParallelization = true)]
#pragma warning disable CA1515, CA1711, MA0048 // xUnit needs a public definition; it holds no code
public sealed class ProcessStateCollection
{
}
#pragma warning restore CA1515, CA1711, MA0048

[Collection(nameof(ProcessStateCollection))]
public sealed class BacklogTagsTests : IDisposable
{
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

    [Theory]
    [InlineData("push", "tags: [\"a;b\"]", "must not contain")]
    [InlineData("push", "tags: [\"\"]", "must not be empty")]
    [InlineData("push", "tags: a", "Invalid frontmatter")]
    [InlineData("create", "tags: [\"a;b\"]", "must not contain")]
    [InlineData("create", "tags: [\"\"]", "must not be empty")]
    [InlineData("create", "tags: a", "Invalid frontmatter")]
    public async Task InvokeAsync_InvalidTags_Returns3BeforeAnyRequest(string command, string line, string message)
    {
        await File.WriteAllTextAsync(_path, $"---\nid: 0\ntype: Task\ntitle: T\nparentId: 1\n{line}\n---\n\nBody\n", TestContext.Current.CancellationToken);
        using var services = new ServiceCollection()
            .AddLogging()
            .AddSingleton(new QuillConfig())
            .AddSingleton<HttpClient>()
            .AddSingleton<IAzureDevOpsClient>(sp => new AzureDevOpsClient(sp.GetRequiredService<HttpClient>(), "https://devops.example.com", "COL", "PROJ"))
            .AddSingleton<IIdentityClient>(sp => new AzureDevOpsIdentityClient(sp.GetRequiredService<HttpClient>(), "https://devops.example.com", "COL"))
            .BuildServiceProvider();
        Console.SetOut(_output);

        var exitCode = await CliHost.InvokeAsync(["backlog", command, _path], services);

        exitCode.ShouldBe(3);
        _output.ToString().ShouldContain(message);
    }
}
