using Microsoft.Extensions.DependencyInjection;
using Quill.Core;
using Quill.Core.Models;
using Quill.Tests.Shared.Fakes;
using Shouldly;

namespace Quill.Cli.Tests;

[Collection(nameof(ProcessStateCollection))]
public sealed class ActionFrameTests : IDisposable
{
#pragma warning disable CA2213 // The process owns the original Console.Out; this class only puts it back
    private readonly TextWriter _originalOut = Console.Out;
#pragma warning restore CA2213
    private readonly StringWriter _output = new();

    public ActionFrameTests() => Console.SetOut(_output);

    public void Dispose()
    {
        Console.SetOut(_originalOut);
        Environment.ExitCode = 0;
        _output.Dispose();
    }

    [Fact]
    public async Task InvokeAsync_PipelineTests_PrintsTheRowsFromTheRegisteredFake()
    {
        var fake = new ScriptedBuildClient
        {
            ListTestRuns = _ => [new TestRun { Id = 7, Name = "Unit", State = "Completed", TotalTests = 1, PassedTests = 1, UnanalyzedTests = 0, NotApplicableTests = 0 }],
        };
        await using var services = new ServiceCollection().AddSingleton<IAzureDevOpsBuildClient>(fake).BuildServiceProvider();

        var exitCode = await CliHost.InvokeAsync(["pipeline", "tests", "4201"], services);

        exitCode.ShouldBe(0);
        _output.ToString().ShouldStartWith("[{\"id\":7,");
    }

    [Fact]
    public async Task InvokeAsync_ActionRejected_PrintsTheErrorRowAndExits1()
    {
        var fake = new ScriptedBuildClient { ListTestRuns = _ => throw new RejectedException("Run 4201 was deleted (400)") };
        await using var services = new ServiceCollection().AddSingleton<IAzureDevOpsBuildClient>(fake).BuildServiceProvider();

        var exitCode = await CliHost.InvokeAsync(["pipeline", "tests", "4201"], services);

        exitCode.ShouldBe(1);
        _output.ToString().ShouldBe("{\"error\":\"Run 4201 was deleted (400)\",\"code\":1}" + Environment.NewLine);
    }

    [Fact]
    public async Task InvokeAsync_ConfigMissing_PrintsTheErrorRowAndExits3()
    {
        await using var services = new ServiceCollection()
            .AddSingleton<IAzureDevOpsBuildClient>(_ => throw new UsageException("Config file not found: .quill.json"))
            .BuildServiceProvider();

        var exitCode = await CliHost.InvokeAsync(["pipeline", "tests", "4201"], services);

        exitCode.ShouldBe(3);
        _output.ToString().ShouldContain("\"code\":3");
    }
}
