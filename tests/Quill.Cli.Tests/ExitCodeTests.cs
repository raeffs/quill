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

    [Theory]
    [InlineData("0")]
    [InlineData("4202", "--timeout", "0")]
    [InlineData("4202", "--timeout", "-1")]
    public async Task InvokeAsync_PipelineWaitBadInput_Returns3(params string[] args)
    {
        var exitCode = await CliHost.InvokeAsync(["pipeline", "wait", .. args], TestServices.Empty);

        exitCode.ShouldBe(3);
    }

    [Theory]
    [InlineData("cancel", "0")]
    [InlineData("cancel", "-1")]
    [InlineData("retry", "0")]
    [InlineData("retry", "abc")]
    public async Task InvokeAsync_PipelineCancelRetryBadInput_Returns3(params string[] args)
    {
        var exitCode = await CliHost.InvokeAsync(["pipeline", .. args], TestServices.Empty);

        exitCode.ShouldBe(3);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("4201", "--limit", "5")]
    [InlineData("4201", "--skip", "0")]
    [InlineData("4201", "--outcome", "Failed")]
    [InlineData("4201", "--with-results", "--limit", "0")]
    public async Task InvokeAsync_PipelineTestsBadInput_Returns3(params string[] args)
    {
        var exitCode = await CliHost.InvokeAsync(["pipeline", "tests", .. args], TestServices.Empty);

        exitCode.ShouldBe(3);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("abc")]
    public async Task InvokeAsync_PrChecksBadInput_Returns3(params string[] args)
    {
        var exitCode = await CliHost.InvokeAsync(["pr", "checks", .. args], TestServices.Empty);

        exitCode.ShouldBe(3);
    }

    [Theory]
    [InlineData("5", "--depth", "abc")]
    [InlineData("5", "--depth")]
    [InlineData("5", "--depth", "0")]
    public async Task InvokeAsync_WiTreeBadDepth_Returns3(params string[] args)
    {
        var exitCode = await CliHost.InvokeAsync(["wi", "tree", .. args], TestServices.Empty);

        exitCode.ShouldBe(3);
    }

    [Theory]
    [InlineData("wi", "view", "0")]
    [InlineData("wi", "comments", "0")]
    [InlineData("wi", "tree", "0")]
    [InlineData("wi", "list", "--assignee", "@me", "--limit", "0")]
    [InlineData("backlog", "pull", "0", "item.md")]
    [InlineData("pr", "view", "0")]
    [InlineData("pr", "threads", "0")]
    [InlineData("pr", "revisions", "0")]
    [InlineData("pr", "list", "--limit", "0")]
    [InlineData("wi", "view", "-5")]
    [InlineData("wi", "create", "--type", "Task", "--title", "Write the importer", "--parent", "0")]
    [InlineData("wi", "create", "--type", "Task", "--title", "Write the importer", "--parent", "-5")]
    public async Task InvokeAsync_IdOrLimitBelowOne_Returns3(params string[] args)
    {
        var exitCode = await CliHost.InvokeAsync(args, TestServices.Empty);

        exitCode.ShouldBe(3);
    }

    [Theory]
    [InlineData("--repo", "", "--source-branch", "feature/login", "--title", "Fix the login")]
    [InlineData("--repo", " ", "--source-branch", "feature/login", "--title", "Fix the login")]
    [InlineData("--repo", "Contoso", "--source-branch", "", "--title", "Fix the login")]
    [InlineData("--repo", "Contoso", "--source-branch", " ", "--title", "Fix the login")]
    [InlineData("--repo", "Contoso", "--source-branch", "feature/login", "--title", "")]
    [InlineData("--repo", "Contoso", "--source-branch", "feature/login", "--title", " ")]
    public async Task InvokeAsync_PrCreateEmptyValue_Returns3(params string[] args)
    {
        var exitCode = await CliHost.InvokeAsync(["pr", "create", .. args], TestServices.Empty);

        exitCode.ShouldBe(3);
    }
}
