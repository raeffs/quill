using System.Text.Json;
using Quill.Core.Models;

namespace Quill.Core.Config;

public static class ConfigLoader
{
    public static QuillConfig Load(string directory)
    {
        var homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Load(directory, homeDirectory);
    }

    internal static QuillConfig Load(string directory, string fallbackDirectory)
    {
        var filePath = ResolveConfigFile(directory, fallbackDirectory)
            ?? throw new FileNotFoundException(
                $"Configuration file not found: .quill.json (searched in {directory} and {fallbackDirectory})");

        var json = File.ReadAllText(filePath);
        var config = JsonSerializer.Deserialize(json, ConfigJsonContext.Default.QuillConfig)
            ?? throw new InvalidOperationException("Failed to parse .quill.json");

        if (string.IsNullOrWhiteSpace(config.ServerUrl))
        {
            throw new InvalidOperationException("serverUrl is required in .quill.json");
        }

        if (!config.ServerUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("serverUrl must use HTTPS.");
        }

        if (string.IsNullOrWhiteSpace(config.Collection))
        {
            throw new InvalidOperationException("collection is required in .quill.json");
        }

        if (string.IsNullOrWhiteSpace(config.Project))
        {
            throw new InvalidOperationException("project is required in .quill.json");
        }

        if (config.AllowedStates is null || config.AllowedStates.Count == 0)
        {
            throw new InvalidOperationException("allowedStates is required in .quill.json");
        }

        if (config.AllowedParentStates is null || config.AllowedParentStates.Count == 0)
        {
            throw new InvalidOperationException("allowedParentStates is required in .quill.json");
        }

        return config;
    }

    private static string? ResolveConfigFile(params string[] directories)
    {
        foreach (var directory in directories)
        {
            var filePath = Path.Combine(directory, ".quill.json");
            if (File.Exists(filePath))
            {
                return filePath;
            }
        }

        return null;
    }
}
