using System.Net;
using Quill.Tests.Shared;
using Shouldly;

namespace Quill.AzureDevOps.Tests;

public class AzureDevOpsPullRequestClientDiffStatsTests
{
    [Fact]
    public async Task GetDiffStatsAsync_NoIterations_ReturnsEmptyStats()
    {
        // Arrange
        using var handler = new RecordingHandler(
        [
            new FakeResponse(HttpStatusCode.OK, """{"value":[]}"""),
        ]);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsPullRequestClient(
            httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        // Act
        var stats = await client.GetDiffStatsAsync(4711, "importer", TestContext.Current.CancellationToken);

        // Assert
        stats.TotalFiles.ShouldBe(0);
        stats.TotalAdded.ShouldBe(0);
        stats.TotalRemoved.ShouldBe(0);
        stats.Files.ShouldBeEmpty();
        handler.Requests.Count.ShouldBe(1);
    }

    [Fact]
    public async Task GetDiffStatsAsync_NoChanges_ReturnsEmptyStats()
    {
        // Arrange
        var iterations = """
        {
            "value": [{
                "id": 1,
                "sourceRefCommit": {"commitId": "abc"},
                "commonRefCommit": {"commitId": "def"}
            }]
        }
        """;
        using var handler = new RecordingHandler(
        [
            new FakeResponse(HttpStatusCode.OK, iterations),
            new FakeResponse(HttpStatusCode.OK, """{"changeEntries":[]}"""),
        ]);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsPullRequestClient(
            httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        // Act
        var stats = await client.GetDiffStatsAsync(4711, "importer", TestContext.Current.CancellationToken);

        // Assert
        stats.TotalFiles.ShouldBe(0);
        stats.Files.ShouldBeEmpty();
        handler.Requests.Count.ShouldBe(2);
    }

    [Fact]
    public async Task GetDiffStatsAsync_EditAndDeleteAndAdd_AggregatesLineCounts()
    {
        // Arrange
        var iterations = """
        {
            "value": [{
                "id": 2,
                "sourceRefCommit": {"commitId": "head"},
                "commonRefCommit": {"commitId": "base"}
            }]
        }
        """;
        var changes = """
        {
            "changeEntries": [
                {"changeType": "edit", "item": {"path": "/src/Foo.cs", "isFolder": false}},
                {"changeType": "delete", "item": {"path": "/src/Old.cs", "isFolder": false}},
                {"changeType": "add", "item": {"path": "/src/New.cs", "isFolder": false}}
            ]
        }
        """;
        var fileDiffs = """
        [
            {
                "path": "/src/Foo.cs",
                "originalPath": "/src/Foo.cs",
                "binaryContent": false,
                "lineCharBlocks": [
                    {"changeType": 3, "modified": {"startLine": 10, "lineCount": 12}, "original": {"startLine": 10, "lineCount": 3}}
                ]
            },
            {
                "path": "",
                "originalPath": "/src/Old.cs",
                "binaryContent": false,
                "lineCharBlocks": [
                    {"changeType": 2, "original": {"startLine": 1, "lineCount": 48}}
                ]
            },
            {
                "path": "/src/New.cs",
                "originalPath": "",
                "binaryContent": false,
                "lineCharBlocks": [
                    {"changeType": 1, "modified": {"startLine": 1, "lineCount": 7}}
                ]
            }
        ]
        """;
        using var handler = new RecordingHandler(
        [
            new FakeResponse(HttpStatusCode.OK, iterations),
            new FakeResponse(HttpStatusCode.OK, changes),
            new FakeResponse(HttpStatusCode.OK, fileDiffs),
        ]);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsPullRequestClient(
            httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        // Act
        var stats = await client.GetDiffStatsAsync(4711, "importer", TestContext.Current.CancellationToken);

        // Assert
        stats.TotalFiles.ShouldBe(3);
        stats.TotalAdded.ShouldBe(19);
        stats.TotalRemoved.ShouldBe(51);
        var foo = stats.Files.Single(f => string.Equals(f.Path, "src/Foo.cs", StringComparison.Ordinal));
        foo.ChangeType.ShouldBe("edit");
        foo.Added.ShouldBe(12);
        foo.Removed.ShouldBe(3);
        var old = stats.Files.Single(f => string.Equals(f.Path, "src/Old.cs", StringComparison.Ordinal));
        old.ChangeType.ShouldBe("delete");
        old.Removed.ShouldBe(48);
        var @new = stats.Files.Single(f => string.Equals(f.Path, "src/New.cs", StringComparison.Ordinal));
        @new.ChangeType.ShouldBe("add");
        @new.Added.ShouldBe(7);
    }

    [Fact]
    public async Task GetDiffStatsAsync_Rename_KeepsOldPathAndZeroCounts()
    {
        // Arrange
        var iterations = """{"value":[{"id":1,"sourceRefCommit":{"commitId":"h"},"commonRefCommit":{"commitId":"b"}}]}""";
        var changes = """
        {
            "changeEntries": [
                {"changeType": "rename", "item": {"path": "/src/Bar.cs", "isFolder": false}, "originalPath": "/src/Baz.cs"}
            ]
        }
        """;
        var fileDiffs = """[]""";
        using var handler = new RecordingHandler(
        [
            new FakeResponse(HttpStatusCode.OK, iterations),
            new FakeResponse(HttpStatusCode.OK, changes),
            new FakeResponse(HttpStatusCode.OK, fileDiffs),
        ]);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsPullRequestClient(
            httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        // Act
        var stats = await client.GetDiffStatsAsync(4711, "importer", TestContext.Current.CancellationToken);

        // Assert
        var bar = stats.Files.ShouldHaveSingleItem();
        bar.Path.ShouldBe("src/Bar.cs");
        bar.ChangeType.ShouldBe("rename");
        bar.OldPath.ShouldBe("src/Baz.cs");
        bar.Added.ShouldBe(0);
        bar.Removed.ShouldBe(0);
    }

    [Fact]
    public async Task GetDiffStatsAsync_BinaryFileByExtension_ReportsZeroAndBinaryFlag()
    {
        // Arrange
        var iterations = """{"value":[{"id":1,"sourceRefCommit":{"commitId":"h"},"commonRefCommit":{"commitId":"b"}}]}""";
        var changes = """
        {
            "changeEntries": [
                {"changeType": "edit", "item": {"path": "/assets/logo.png", "isFolder": false}}
            ]
        }
        """;

        // Even if ADO returned line blocks, the extension-based detection wins and zeroes the counts.
        var fileDiffs = """
        [
            {
                "path": "/assets/logo.png",
                "originalPath": "/assets/logo.png",
                "binaryContent": false,
                "lineCharBlocks": [
                    {"changeType": 3, "modified": {"startLine": 1, "lineCount": 999}, "original": {"startLine": 1, "lineCount": 999}}
                ]
            }
        ]
        """;
        using var handler = new RecordingHandler(
        [
            new FakeResponse(HttpStatusCode.OK, iterations),
            new FakeResponse(HttpStatusCode.OK, changes),
            new FakeResponse(HttpStatusCode.OK, fileDiffs),
        ]);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsPullRequestClient(
            httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        // Act
        var stats = await client.GetDiffStatsAsync(4711, "importer", TestContext.Current.CancellationToken);

        // Assert
        var logo = stats.Files.ShouldHaveSingleItem();
        logo.Binary.ShouldBeTrue();
        logo.Added.ShouldBe(0);
        logo.Removed.ShouldBe(0);
        stats.TotalAdded.ShouldBe(0);
        stats.TotalRemoved.ShouldBe(0);
    }

    [Fact]
    public async Task GetDiffStatsAsync_BinaryContentFlag_ReportsZeroAndBinaryFlag()
    {
        // Arrange
        var iterations = """{"value":[{"id":1,"sourceRefCommit":{"commitId":"h"},"commonRefCommit":{"commitId":"b"}}]}""";
        var changes = """
        {
            "changeEntries": [
                {"changeType": "edit", "item": {"path": "/data/blob.dat", "isFolder": false}}
            ]
        }
        """;
        var fileDiffs = """
        [
            {
                "path": "/data/blob.dat",
                "originalPath": "/data/blob.dat",
                "binaryContent": true,
                "lineCharBlocks": []
            }
        ]
        """;
        using var handler = new RecordingHandler(
        [
            new FakeResponse(HttpStatusCode.OK, iterations),
            new FakeResponse(HttpStatusCode.OK, changes),
            new FakeResponse(HttpStatusCode.OK, fileDiffs),
        ]);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsPullRequestClient(
            httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        // Act
        var stats = await client.GetDiffStatsAsync(4711, "importer", TestContext.Current.CancellationToken);

        // Assert
        stats.Files.ShouldHaveSingleItem().Binary.ShouldBeTrue();
    }

    [Fact]
    public async Task GetDiffStatsAsync_LatestIterationUsed()
    {
        // Arrange
        var iterations = """
        {
            "value": [
                {"id": 1, "sourceRefCommit": {"commitId": "old"}, "commonRefCommit": {"commitId": "b"}},
                {"id": 2, "sourceRefCommit": {"commitId": "new"}, "commonRefCommit": {"commitId": "b2"}}
            ]
        }
        """;
        using var handler = new RecordingHandler(
        [
            new FakeResponse(HttpStatusCode.OK, iterations),
            new FakeResponse(HttpStatusCode.OK, """{"changeEntries":[]}"""),
        ]);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsPullRequestClient(
            httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        // Act
        var stats = await client.GetDiffStatsAsync(4711, "importer", TestContext.Current.CancellationToken);

        // Assert
        stats.Files.ShouldBeEmpty();
        var changesUrl = handler.Requests[1].Url!;
        changesUrl.ShouldContain("/iterations/2/changes");
    }

    [Fact]
    public async Task GetDiffStatsAsync_FolderEntries_AreSkipped()
    {
        // Arrange
        var iterations = """{"value":[{"id":1,"sourceRefCommit":{"commitId":"h"},"commonRefCommit":{"commitId":"b"}}]}""";
        var changes = """
        {
            "changeEntries": [
                {"changeType": "add", "item": {"path": "/src/NewDir", "isFolder": true}},
                {"changeType": "edit", "item": {"path": "/src/Foo.cs", "isFolder": false}}
            ]
        }
        """;
        var fileDiffs = """
        [
            {
                "path": "/src/Foo.cs",
                "originalPath": "/src/Foo.cs",
                "lineCharBlocks": [{"changeType": 3, "modified": {"startLine": 1, "lineCount": 2}, "original": {"startLine": 1, "lineCount": 1}}]
            }
        ]
        """;
        using var handler = new RecordingHandler(
        [
            new FakeResponse(HttpStatusCode.OK, iterations),
            new FakeResponse(HttpStatusCode.OK, changes),
            new FakeResponse(HttpStatusCode.OK, fileDiffs),
        ]);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsPullRequestClient(
            httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        // Act
        var stats = await client.GetDiffStatsAsync(4711, "importer", TestContext.Current.CancellationToken);

        // Assert
        stats.TotalFiles.ShouldBe(1);
        stats.Files.ShouldHaveSingleItem().Path.ShouldBe("src/Foo.cs");
    }
}
