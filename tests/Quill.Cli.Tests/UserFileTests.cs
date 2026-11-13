using Quill.Core;
using Shouldly;

namespace Quill.Cli.Tests;

public sealed class UserFileTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

    public UserFileTests() => Directory.CreateDirectory(_directory);

    public void Dispose() => Directory.Delete(_directory, true);

    [Fact]
    public async Task ReadAllTextAsync_PathIsADirectory_ThrowsUsageException()
    {
        var ex = await Should.ThrowAsync<UsageException>(
            () => UserFile.ReadAllTextAsync(_directory, TestContext.Current.CancellationToken));

        ex.Message.ShouldStartWith("Cannot read ");
        ex.Message.ShouldContain(_directory);
        (ex.InnerException is IOException or UnauthorizedAccessException).ShouldBeTrue();
    }

    [Fact]
    public async Task WriteAllTextAsync_ParentDirectoryMissing_ThrowsUsageException()
    {
        var path = Path.Combine(_directory, "missing", "item.md");

        var ex = await Should.ThrowAsync<UsageException>(
            () => UserFile.WriteAllTextAsync(path, "text", TestContext.Current.CancellationToken));

        ex.Message.ShouldStartWith("Cannot write ");
        ex.Message.ShouldContain(path);
        ex.InnerException.ShouldBeAssignableTo<IOException>();
    }

    [Fact]
    public async Task ReadAllTextAsync_ExistingFile_ReturnsItsText()
    {
        var path = Path.Combine(_directory, "item.md");
        await File.WriteAllTextAsync(path, "Grüße, 世界", TestContext.Current.CancellationToken);

        var text = await UserFile.ReadAllTextAsync(path, TestContext.Current.CancellationToken);

        text.ShouldBe("Grüße, 世界");
    }

    [Fact]
    public async Task WriteAllTextAsync_RoundTripsTextAndAddsNoByteOrderMark()
    {
        var path = Path.Combine(_directory, "item.md");

        await UserFile.WriteAllTextAsync(path, "Grüße, 世界\n", TestContext.Current.CancellationToken);

        (await UserFile.ReadAllTextAsync(path, TestContext.Current.CancellationToken)).ShouldBe("Grüße, 世界\n");
        (await File.ReadAllBytesAsync(path, TestContext.Current.CancellationToken)).ShouldBe(System.Text.Encoding.UTF8.GetBytes("Grüße, 世界\n"));
    }

    [Fact]
    public async Task CreateDirectory_ParentIsAFile_ThrowsUsageException()
    {
        var file = Path.Combine(_directory, "item.md");
        await File.WriteAllTextAsync(file, "text", TestContext.Current.CancellationToken);
        var path = Path.Combine(file, "sub");

        var ex = Should.Throw<UsageException>(() => UserFile.CreateDirectory(path));

        ex.Message.ShouldStartWith("Cannot write ");
        ex.Message.ShouldContain(path);
        (ex.InnerException is IOException or UnauthorizedAccessException).ShouldBeTrue();
    }

    [Fact]
    public void CreateDirectory_NewAndExistingDirectory_Succeeds()
    {
        var path = Path.Combine(_directory, "a", "b");

        UserFile.CreateDirectory(path);
        UserFile.CreateDirectory(path);

        Directory.Exists(path).ShouldBeTrue();
    }
}
