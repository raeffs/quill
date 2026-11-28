using System.Runtime.Versioning;
using Shouldly;

namespace Quill.Core.Tests;

public sealed class FilePersonStoreTests : IDisposable
{
    private static readonly DateTimeOffset Saved = new(2026, 11, 28, 10, 30, 0, TimeSpan.Zero);

    private readonly string _root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

    private string CacheDirectory => Path.Combine(_root, "quill");

    private string FilePath => Path.Combine(CacheDirectory, "people.json");

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    [Fact]
    public void Load_NoFile_IsEmpty() =>
        new FilePersonStore(FilePath).Load().ShouldBeEmpty();

    [Fact]
    public void SaveThenLoad_RoundTripsEveryField()
    {
        var entries = Entries(
            ("a", new("id-1", "Jane Doe", @"CONTOSO\jdoe", false, Saved.AddTicks(1234567))),
            ("b", new("id-3", @"[Fabrikam]\Contoso Platform", null, true, Saved)));
        var store = new FilePersonStore(FilePath);

        store.Save(entries);

        store.Load().ShouldBe(entries, ignoreOrder: true);
    }

    [Fact]
    public void Save_WritesTheVersion()
    {
        new FilePersonStore(FilePath).Save(Entries());

        File.ReadAllText(FilePath).ShouldContain("\"version\":2");
    }

    [Fact]
    public void Save_CreatesTheDirectory()
    {
        new FilePersonStore(FilePath).Save(Entries());

        Directory.Exists(CacheDirectory).ShouldBeTrue();
    }

    [Fact]
    public void Save_LeavesNoTemporaryFile()
    {
        new FilePersonStore(FilePath).Save(Entries(("a", new("id-1", "Jane Doe", null, false, Saved))));

        Directory.GetFiles(CacheDirectory).ShouldBe([FilePath]);
    }

    [Fact]
    public void Save_ReplacesTheFile()
    {
        var store = new FilePersonStore(FilePath);
        var second = Entries(("b", new("id-2", "John Roe", null, false, Saved)));
        store.Save(Entries(("a", new("id-1", "Jane Doe", null, false, Saved))));

        store.Save(second);

        store.Load().ShouldBe(second);
    }

    [Fact]
    [UnsupportedOSPlatform("windows")]
    public void Save_FileAndDirectoryAreForTheUserOnly()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "Unix file modes do not exist on Windows.");
        new FilePersonStore(FilePath).Save(Entries());

        File.GetUnixFileMode(FilePath).ShouldBe(UnixFileMode.UserRead | UnixFileMode.UserWrite);
        File.GetUnixFileMode(CacheDirectory).ShouldBe(UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not json")]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("{\"version\":1}")]
    [InlineData("{\"version\":1,\"entries\":null}")]
    [InlineData("{\"version\":3,\"entries\":{}}")]
    [InlineData("{\"version\":\"1\",\"entries\":{}}")]
    public void Load_BrokenFile_IsEmpty(string text)
    {
        Directory.CreateDirectory(CacheDirectory);
        File.WriteAllText(FilePath, text);

        new FilePersonStore(FilePath).Load().ShouldBeEmpty();
    }

    [Fact]
    public void Load_EntryWithoutAnId_SkipsThatEntry()
    {
        Directory.CreateDirectory(CacheDirectory);
        File.WriteAllText(
            FilePath,
            """
            {"version":2,"entries":{
              "a":{"id":null,"displayName":"x"},
              "b":null,
              "c":{"id":"id-1","displayName":"Jane Doe","uniqueName":null,"savedAt":"2026-11-28T10:30:00+00:00"}}}
            """);

        var loaded = new FilePersonStore(FilePath).Load();

        loaded.Keys.ShouldBe(["c"]);
    }

    [Fact]
    public void Load_FileOfVersion1_IsEmpty()
    {
        Directory.CreateDirectory(CacheDirectory);
        File.WriteAllText(FilePath, """{"version":1,"entries":{"HTTPS://DEVOPS.EXAMPLE.COM|COL|person|PLATFORM":{"id":"id-2","displayName":"John Roe","uniqueName":"CONTOSO\\jroe","savedAt":"2026-11-28T10:00:00+00:00"}}}""");

        new FilePersonStore(FilePath).Load().ShouldBeEmpty();
    }

    [Fact]
    public void Load_PathIsACacheDirectoryIsEmpty()
    {
        Directory.CreateDirectory(FilePath);

        new FilePersonStore(FilePath).Load().ShouldBeEmpty();
    }

    [Fact]
    public void Save_ParentIsAFile_DoesNotThrow()
    {
        Directory.CreateDirectory(_root);
        File.WriteAllText(CacheDirectory, "x");
        var store = new FilePersonStore(FilePath);

        Should.NotThrow(() => store.Save(Entries(("a", new("id-1", "Jane Doe", null, false, Saved)))));
    }

    [Fact]
    public void NullPath_LoadsEmptyAndSavesNothing()
    {
        var store = new FilePersonStore(null);

        store.Save(Entries(("a", new("id-1", "Jane Doe", null, false, Saved))));

        store.Load().ShouldBeEmpty();
    }

    private static Dictionary<string, PersonEntry> Entries(params (string Key, PersonEntry Entry)[] items) =>
        items.ToDictionary(item => item.Key, item => item.Entry, StringComparer.Ordinal);
}
