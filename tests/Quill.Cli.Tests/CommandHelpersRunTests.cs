using Quill.Core;
using Quill.Core.Models;
using Shouldly;

namespace Quill.Cli.Tests;

[Collection(nameof(ProcessStateCollection))]
public sealed class CommandHelpersRunTests : IDisposable
{
#pragma warning disable CA2213 // The process owns the original Console.Out; this class only puts it back
    private readonly TextWriter _originalOut = Console.Out;
#pragma warning restore CA2213
    private readonly StringWriter _output = new();

    public CommandHelpersRunTests() => Console.SetOut(_output);

    public void Dispose()
    {
        Console.SetOut(_originalOut);
        Environment.ExitCode = 0;
        _output.Dispose();
    }

    [Theory]
    [InlineData(typeof(FileNotFoundException), 3)]
    [InlineData(typeof(InvalidOperationException), 3)]
    [InlineData(typeof(InvalidRequestException), 1)]
    [InlineData(typeof(PipelineRejectedException), 1)]
    [InlineData(typeof(HttpRequestException), 2)]
    public async Task RunAsync_ActionThrows_WritesTheErrorRowAndSetsExitCode(Type exceptionType, int code)
    {
        var exception = (Exception)Activator.CreateInstance(exceptionType, "boom")!;

        await CommandHelpers.RunAsync<CreateResult>(
            _ => throw exception,
            CommandHelpers.Context.CreateResult,
            TestContext.Current.CancellationToken);

        Environment.ExitCode.ShouldBe(code);
        _output.ToString().ShouldContain($"\"code\":{code}");
    }
}
