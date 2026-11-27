using Shouldly;

namespace Quill.Cli.Tests;

[Collection(nameof(ProcessStateCollection))]
public sealed class UserFileStdinTests
{
    [Theory]
    [InlineData("Grüße, 世界\n")]
    [InlineData("")]
    public async Task ReadAllTextOrStdinAsync_PathIsADash_ReturnsStdin(string piped)
    {
        var stdin = Console.In;
        using var reader = new StringReader(piped);
        Console.SetIn(reader);
        try
        {
            var text = await UserFile.ReadAllTextOrStdinAsync("-", TestContext.Current.CancellationToken);

            text.ShouldBe(piped);
        }
        finally
        {
            Console.SetIn(stdin);
        }
    }
}
