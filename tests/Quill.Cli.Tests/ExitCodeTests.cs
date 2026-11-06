using Shouldly;

namespace Quill.Cli.Tests;

public class ExitCodeTests
{
    [Fact]
    public async Task InvokeAsync_MissingRequiredFlag_Returns3()
    {
        var exitCode = await CliHost.InvokeAsync(["pr", "create", "--repo", "x", "--title", "y"], TestServices.Empty);

        exitCode.ShouldBe(3);
    }

    [Fact]
    public async Task InvokeAsync_UnknownCommand_Returns3()
    {
        var exitCode = await CliHost.InvokeAsync(["wi", "nope"], TestServices.Empty);

        exitCode.ShouldBe(3);
    }

    [Fact]
    public async Task InvokeAsync_Help_Returns0()
    {
        var exitCode = await CliHost.InvokeAsync(["--help"], TestServices.Empty);

        exitCode.ShouldBe(0);
    }

    [Theory]
    [InlineData("--start-line", "0")]
    [InlineData("--start-line", "10", "--end-line", "9")]
    [InlineData("--tail", "5", "--start-line", "1")]
    [InlineData("--end-line", "abc")]
    [InlineData("--tail", "abc", "--start-line", "1")]
    public async Task InvokeAsync_PipelineLogBadFlags_Returns3(params string[] flags)
    {
        var exitCode = await CliHost.InvokeAsync(["pipeline", "log", "4201", "12", .. flags], TestServices.Empty);

        exitCode.ShouldBe(3);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("42", "--parameter", "noequals")]
    [InlineData("42", "--parameter", "env=dev", "--parameter", "env=prod")]
    [InlineData("42", "--branch", "")]
    public async Task InvokeAsync_PipelineTriggerBadInput_Returns3(params string[] args)
    {
        var exitCode = await CliHost.InvokeAsync(["pipeline", "trigger", .. args], TestServices.Empty);

        exitCode.ShouldBe(3);
    }
}
