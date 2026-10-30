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
}
