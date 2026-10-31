using Microsoft.Extensions.DependencyInjection;
using Quill.Cli.Commands.Backlog;
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
    [InlineData("tags: []")]
    [InlineData("tags: [a]")]
    public void UpdatedFieldsFor_WithTagsKey_ListsTags(string line)
    {
        var parsed = FrontmatterParser.Parse($"---\nid: 1\ntitle: T\n{line}\n---\n\nBody\n");

        PushCommand.UpdatedFieldsFor(parsed).ShouldBe(["title", "description", "tags"]);
    }

    [Fact]
    public void UpdatedFieldsFor_WithoutTagsKey_OmitsTags()
    {
        var parsed = FrontmatterParser.Parse("---\nid: 1\ntitle: T\n---\n\nBody\n");

        PushCommand.UpdatedFieldsFor(parsed).ShouldBe(["title", "description"]);
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
        using var services = new ServiceCollection().AddLogging().AddSingleton(new QuillConfig()).BuildServiceProvider();
        Console.SetOut(_output);

        var exitCode = await CliHost.InvokeAsync(["backlog", command, _path], services);

        // The message matters: the unregistered client also ends in exit 3, so
        // the code alone would pass even if the tags were never looked at.
        exitCode.ShouldBe(3);
        _output.ToString().ShouldContain(message);
    }
}
