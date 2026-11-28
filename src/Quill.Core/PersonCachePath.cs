namespace Quill.Core;

public static class PersonCachePath
{
    private const string DirectoryName = "quill";

    private const string FileName = "people.json";

    public static string? Default() => Resolve(
        OperatingSystem.IsWindows(),
        Environment.GetEnvironmentVariable("XDG_CACHE_HOME"),
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));

    internal static string? Resolve(bool windows, string? xdgCacheHome, string home, string localAppData)
    {
        if (windows)
        {
            return string.IsNullOrEmpty(localAppData) ? null : Path.Combine(localAppData, DirectoryName, FileName);
        }

        if (!string.IsNullOrEmpty(xdgCacheHome) && Path.IsPathRooted(xdgCacheHome))
        {
            return Path.Combine(xdgCacheHome, DirectoryName, FileName);
        }

        return string.IsNullOrEmpty(home) ? null : Path.Combine(home, ".cache", DirectoryName, FileName);
    }
}
