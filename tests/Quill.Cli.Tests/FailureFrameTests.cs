using System.Text.Json;
using Quill.Core;
using Quill.Core.Models;
using Shouldly;

namespace Quill.Cli.Tests;

[Collection(nameof(ProcessStateCollection))]
public sealed class FailureFrameTests : IDisposable
{
#pragma warning disable CA2213 // The process owns the original writers; this class only puts them back
    private readonly TextWriter _originalOut = Console.Out;
    private readonly TextWriter _originalError = Console.Error;
#pragma warning restore CA2213
    private readonly StringWriter _output = new();
    private readonly StringWriter _error = new();

    public FailureFrameTests()
    {
        Console.SetOut(_output);
        Console.SetError(_error);
    }

    public void Dispose()
    {
        Console.SetOut(_originalOut);
        Console.SetError(_originalError);
        Environment.ExitCode = 0;
        _output.Dispose();
        _error.Dispose();
    }

    [Fact]
    public async Task RunAsync_RowReturned_WritesTheRowAndLeavesExitCodeZero()
    {
        await CommandHelpers.RunAsync(
            _ => Task.FromResult(new CreateResult { Id = 7, Title = "T" }),
            CommandHelpers.Context.CreateResult,
            TestContext.Current.CancellationToken);

        Environment.ExitCode.ShouldBe(0);
        _output.ToString().ShouldBe("{\"id\":7,\"title\":\"T\"}" + Environment.NewLine);
    }

    [Theory]
    [InlineData(typeof(RejectedException), 1)]
    [InlineData(typeof(UnavailableException), 2)]
    [InlineData(typeof(UsageException), 3)]
    [InlineData(typeof(DeniedException), 5)]
    public async Task RunAsync_ModeException_WritesTheMessageAndItsCode(Type type, int code)
    {
        var exception = (Exception)Activator.CreateInstance(type, "boom")!;

        await CommandHelpers.RunAsync<CreateResult>(
            _ => throw exception,
            CommandHelpers.Context.CreateResult,
            TestContext.Current.CancellationToken);

        Environment.ExitCode.ShouldBe(code);
        _output.ToString().ShouldBe($"{{\"error\":\"boom\",\"code\":{code}}}" + Environment.NewLine);
        _error.ToString().ShouldBeEmpty();
    }

    [Fact]
    public async Task RunAsync_DefectException_WritesTheAdviceAndTheTrace()
    {
        await CommandHelpers.RunAsync<CreateResult>(
            _ => throw new DefectException("Failed to deserialize build response."),
            CommandHelpers.Context.CreateResult,
            TestContext.Current.CancellationToken);

        Environment.ExitCode.ShouldBe(4);
        using var row = JsonDocument.Parse(_output.ToString());
        row.RootElement.GetProperty("error").GetString()
            .ShouldBe("Failed to deserialize build response. " + CommandHelpers.DefectAdvice);
        row.RootElement.GetProperty("code").GetInt32().ShouldBe(4);
        row.RootElement.EnumerateObject().Select(p => p.Name).ShouldBe(["error", "code"]);
        _error.ToString().ShouldStartWith("quill ");
        _error.ToString().ShouldContain("DefectException");
    }

    [Fact]
    public async Task RunAsync_UnclassifiedException_IsADefect()
    {
        await CommandHelpers.RunAsync<CreateResult>(
            _ => throw new NotSupportedException("nope"),
            CommandHelpers.Context.CreateResult,
            TestContext.Current.CancellationToken);

        Environment.ExitCode.ShouldBe(4);
        using var row = JsonDocument.Parse(_output.ToString());
        row.RootElement.GetProperty("error").GetString()
            .ShouldBe("NotSupportedException: nope " + CommandHelpers.DefectAdvice);
        _error.ToString().ShouldContain("System.NotSupportedException: nope");
    }

    [Theory]
    [InlineData(typeof(InvalidOperationException))]
    [InlineData(typeof(FileNotFoundException))]
    public async Task RunAsync_GeneralDotNetException_IsADefect(Type type)
    {
        var exception = (Exception)Activator.CreateInstance(type, "boom")!;

        await CommandHelpers.RunAsync<CreateResult>(
            _ => throw exception,
            CommandHelpers.Context.CreateResult,
            TestContext.Current.CancellationToken);

        Environment.ExitCode.ShouldBe(4);
        using var row = JsonDocument.Parse(_output.ToString());
        row.RootElement.GetProperty("code").GetInt32().ShouldBe(4);
    }

    [Fact]
    public async Task RunAsync_DefectMessageWithQuotesAndNewlines_WritesOneJsonLine()
    {
        await CommandHelpers.RunAsync<CreateResult>(
            _ => throw new FormatException("bad \"value\"\nsecond line"),
            CommandHelpers.Context.CreateResult,
            TestContext.Current.CancellationToken);

        var line = _output.ToString().TrimEnd('\r', '\n');
        line.ShouldNotContain('\n');
        using var row = JsonDocument.Parse(line);
        row.RootElement.GetProperty("error").GetString()!.ShouldContain("bad \"value\"\nsecond line");
    }

    [Fact]
    public async Task RunAsync_CallerCancelled_Propagates()
    {
        using var source = new CancellationTokenSource();
        await source.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(() => CommandHelpers.RunAsync<CreateResult>(
            _ => throw new OperationCanceledException(source.Token),
            CommandHelpers.Context.CreateResult,
            source.Token));

        _output.ToString().ShouldBeEmpty();
        Environment.ExitCode.ShouldBe(0);
    }
}
