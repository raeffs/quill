using Quill.Core.Config;
using Shouldly;

namespace Quill.Core.Tests.Config;

public class ConfigLoaderTests
{
    [Fact]
    public void Load_ValidJson_ReturnsConfig()
    {
        var json = """
        {
          "serverUrl": "https://myserver.com/tfs",
          "collection": "DefaultCollection",
          "project": "MyProject",
          "allowedStates": ["Active", "New"],
          "allowedParentStates": ["New", "Approved"]
        }
        """;

        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        try
        {
            Directory.CreateDirectory(tempDir);
            File.WriteAllText(Path.Combine(tempDir, ".quill.json"), json);

            var config = ConfigLoader.Load(tempDir);

            config.ServerUrl.ShouldBe("https://myserver.com/tfs");
            config.Collection.ShouldBe("DefaultCollection");
            config.Project.ShouldBe("MyProject");
            config.AllowedStates.ShouldBe(["Active", "New"], ignoreOrder: true);
            config.AllowedParentStates.ShouldBe(["New", "Approved"], ignoreOrder: true);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void Load_MissingFile_ThrowsFileNotFoundException()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var fallbackDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        try
        {
            Directory.CreateDirectory(tempDir);
            Directory.CreateDirectory(fallbackDir);

            var act = () => ConfigLoader.Load(tempDir, fallbackDir);

            Should.Throw<FileNotFoundException>(act)
                .Message.ShouldContain(".quill.json");
        }
        finally
        {
            Directory.Delete(tempDir, true);
            Directory.Delete(fallbackDir, true);
        }
    }

    [Fact]
    public void Load_MissingFileInDirectory_FallsBackToFallbackDirectory()
    {
        var json = """
        {
          "serverUrl": "https://myserver.com/tfs",
          "collection": "DefaultCollection",
          "project": "MyProject",
          "allowedStates": ["Active", "New"],
          "allowedParentStates": ["New"]
        }
        """;

        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var fallbackDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        try
        {
            Directory.CreateDirectory(tempDir);
            Directory.CreateDirectory(fallbackDir);
            File.WriteAllText(Path.Combine(fallbackDir, ".quill.json"), json);

            var config = ConfigLoader.Load(tempDir, fallbackDir);

            config.ServerUrl.ShouldBe("https://myserver.com/tfs");
            config.Collection.ShouldBe("DefaultCollection");
            config.Project.ShouldBe("MyProject");
        }
        finally
        {
            Directory.Delete(tempDir, true);
            Directory.Delete(fallbackDir, true);
        }
    }

    [Fact]
    public void Load_FileInBothDirectories_PrefersWorkingDirectory()
    {
        var workingDirJson = """
        {
          "serverUrl": "https://working.com/tfs",
          "collection": "WorkingCollection",
          "project": "WorkingProject",
          "allowedStates": ["Active"],
          "allowedParentStates": ["Approved"]
        }
        """;

        var fallbackJson = """
        {
          "serverUrl": "https://fallback.com/tfs",
          "collection": "FallbackCollection",
          "project": "FallbackProject",
          "allowedStates": ["New"],
          "allowedParentStates": ["New"]
        }
        """;

        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var fallbackDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        try
        {
            Directory.CreateDirectory(tempDir);
            Directory.CreateDirectory(fallbackDir);
            File.WriteAllText(Path.Combine(tempDir, ".quill.json"), workingDirJson);
            File.WriteAllText(Path.Combine(fallbackDir, ".quill.json"), fallbackJson);

            var config = ConfigLoader.Load(tempDir, fallbackDir);

            config.ServerUrl.ShouldBe("https://working.com/tfs");
            config.Collection.ShouldBe("WorkingCollection");
        }
        finally
        {
            Directory.Delete(tempDir, true);
            Directory.Delete(fallbackDir, true);
        }
    }

    [Fact]
    public void Load_HttpUrl_ThrowsInvalidOperationException()
    {
        var json = """
        {
          "serverUrl": "http://myserver.com/tfs",
          "collection": "DefaultCollection",
          "project": "MyProject",
          "allowedStates": ["Active"],
          "allowedParentStates": ["New"]
        }
        """;

        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        try
        {
            Directory.CreateDirectory(tempDir);
            File.WriteAllText(Path.Combine(tempDir, ".quill.json"), json);

            var act = () => ConfigLoader.Load(tempDir);

            Should.Throw<InvalidOperationException>(act)
                .Message.ShouldContain("HTTPS");
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void Load_MissingAllowedParentStates_ThrowsInvalidOperationException()
    {
        var json = """
        {
          "serverUrl": "https://myserver.com/tfs",
          "collection": "DefaultCollection",
          "project": "MyProject",
          "allowedStates": ["Active"]
        }
        """;

        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        try
        {
            Directory.CreateDirectory(tempDir);
            File.WriteAllText(Path.Combine(tempDir, ".quill.json"), json);

            var act = () => ConfigLoader.Load(tempDir);

            Should.Throw<InvalidOperationException>(act)
                .Message.ShouldContain("allowedParentStates");
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void Load_MissingRequiredField_ThrowsInvalidOperationException()
    {
        var json = """
        {
          "serverUrl": "https://myserver.com/tfs"
        }
        """;

        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        try
        {
            Directory.CreateDirectory(tempDir);
            File.WriteAllText(Path.Combine(tempDir, ".quill.json"), json);

            var act = () => ConfigLoader.Load(tempDir);

            Should.Throw<InvalidOperationException>(act);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }
}
