using Quill.Core;

namespace Quill.Cli;

internal static class UserFile
{
    private const string StdinPath = "-";

    public static void CreateDirectory(string path)
    {
        try
        {
            Directory.CreateDirectory(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new UsageException($"Cannot write {path}: {ex.Message}", ex);
        }
    }

    public static async Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken)
    {
        try
        {
            return await File.ReadAllTextAsync(path, cancellationToken);
        }
        catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException or ArgumentException)
        {
            throw new UsageException($"File not found: {path}", ex);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new UsageException($"Cannot read {path}: {ex.Message}", ex);
        }
    }

    public static async Task<string> ReadAllTextOrStdinAsync(string pathOrDash, CancellationToken cancellationToken)
    {
        if (string.Equals(pathOrDash, StdinPath, StringComparison.Ordinal))
        {
            return await Console.In.ReadToEndAsync(cancellationToken);
        }

        return await ReadAllTextAsync(pathOrDash, cancellationToken);
    }

    public static async Task WriteAllTextAsync(string path, string content, CancellationToken cancellationToken)
    {
        try
        {
            await File.WriteAllTextAsync(path, content, cancellationToken);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new UsageException($"Cannot write {path}: {ex.Message}", ex);
        }
    }
}
