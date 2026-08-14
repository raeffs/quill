using Microsoft.Extensions.DependencyInjection;

namespace Quill.Cli.Tests;

internal static class TestServices
{
    // CliHost.Parse only builds the command tree; it never resolves any service.
    // An empty provider is therefore sufficient for parsing-only tests.
    public static IServiceProvider Empty { get; } = new ServiceCollection().BuildServiceProvider();
}
