using System.Security;
using System.Text.Json;

namespace Quill.Core;

public sealed class FilePersonStore(string? path) : IPersonStore
{
    public const int Version = 1;

    public IReadOnlyDictionary<string, PersonEntry> Load()
    {
        var result = new Dictionary<string, PersonEntry>(StringComparer.Ordinal);
        if (path is null)
        {
            return result;
        }

        try
        {
            using var stream = File.OpenRead(path);
            var file = JsonSerializer.Deserialize(stream, PersonCacheJsonContext.Default.PersonCacheFile);
            if (file is not { Entries: { } entries } || file.Version != Version)
            {
                return result;
            }

            foreach (var (key, entry) in entries)
            {
                if (entry is { Id: not null, DisplayName: not null })
                {
                    result[key] = entry;
                }
            }
        }
        catch (Exception exception) when (IsFileProblem(exception))
        {
            result.Clear();
        }

        return result;
    }

    public void Save(IReadOnlyDictionary<string, PersonEntry> entries)
    {
        if (path is null)
        {
            return;
        }

        string? temporary = null;
        try
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                CreateDirectory(directory);
            }

            temporary = $"{path}.{Guid.NewGuid():N}.tmp";
            var file = new PersonCacheFile(Version, entries.ToDictionary(pair => pair.Key, pair => (PersonEntry?)pair.Value, StringComparer.Ordinal));
            using (var stream = CreateFile(temporary))
            {
                JsonSerializer.Serialize(stream, file, PersonCacheJsonContext.Default.PersonCacheFile);
            }

            File.Move(temporary, path, overwrite: true);
            temporary = null;
        }
        catch (Exception exception) when (IsFileProblem(exception))
        {
            Discard(temporary);
        }
    }

    private static void CreateDirectory(string directory)
    {
        if (OperatingSystem.IsWindows())
        {
            Directory.CreateDirectory(directory);
            return;
        }

        Directory.CreateDirectory(directory, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
    }

    private static FileStream CreateFile(string file)
    {
        var options = new FileStreamOptions { Mode = FileMode.CreateNew, Access = FileAccess.Write, Share = FileShare.None };
        if (!OperatingSystem.IsWindows())
        {
            options.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
        }

        return new FileStream(file, options);
    }

    private static void Discard(string? file)
    {
        if (file is null)
        {
            return;
        }

        try
        {
            File.Delete(file);
        }
        catch (Exception exception) when (IsFileProblem(exception))
        {
        }
    }

    private static bool IsFileProblem(Exception exception) =>
        exception is IOException or UnauthorizedAccessException or JsonException or NotSupportedException or SecurityException;
}
