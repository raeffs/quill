using System.Text.Json.Serialization;

namespace Quill.AzureDevOps.Dto;

internal sealed class PullRequestIterationsResponse
{
    [JsonPropertyName("value")]
    public IReadOnlyList<PullRequestIterationResponse> Value { get; init; } = Array.Empty<PullRequestIterationResponse>();
}

internal sealed class PullRequestIterationResponse
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("sourceRefCommit")]
    public PullRequestCommitRef? SourceRefCommit { get; init; }

    [JsonPropertyName("commonRefCommit")]
    public PullRequestCommitRef? CommonRefCommit { get; init; }
}

internal sealed class PullRequestCommitRef
{
    [JsonPropertyName("commitId")]
    public string CommitId { get; init; } = string.Empty;
}

internal sealed class PullRequestIterationChangesResponse
{
    [JsonPropertyName("changeEntries")]
    public IReadOnlyList<PullRequestIterationChangeEntry> ChangeEntries { get; init; } = Array.Empty<PullRequestIterationChangeEntry>();
}

internal sealed class PullRequestIterationChangeEntry
{
    [JsonPropertyName("changeType")]
    public string ChangeType { get; init; } = string.Empty;

    [JsonPropertyName("item")]
    public PullRequestChangeItem? Item { get; init; }

    [JsonPropertyName("originalPath")]
    public string? OriginalPath { get; init; }
}

internal sealed class PullRequestChangeItem
{
    [JsonPropertyName("path")]
    public string Path { get; init; } = string.Empty;

    [JsonPropertyName("isFolder")]
    public bool IsFolder { get; init; }
}

internal sealed class PullRequestFileDiffsRequest
{
    [JsonPropertyName("fileDiffParams")]
    public required IReadOnlyList<PullRequestFileDiffParam> FileDiffParams { get; init; }
}

internal sealed class PullRequestFileDiffParam
{
    [JsonPropertyName("path")]
    public required string Path { get; init; }

    [JsonPropertyName("originalPath")]
    public required string OriginalPath { get; init; }
}

internal sealed class PullRequestFileDiffEntry
{
    [JsonPropertyName("path")]
    public string Path { get; init; } = string.Empty;

    [JsonPropertyName("originalPath")]
    public string? OriginalPath { get; init; }

    [JsonPropertyName("binaryContent")]
    public bool BinaryContent { get; init; }

    [JsonPropertyName("lineCharBlocks")]
    public IReadOnlyList<PullRequestLineCharBlock> LineCharBlocks { get; init; } = Array.Empty<PullRequestLineCharBlock>();
}

internal sealed class PullRequestLineCharBlock
{
    [JsonPropertyName("changeType")]
    public int ChangeType { get; init; }

    [JsonPropertyName("modified")]
    public PullRequestDiffBlockSide? Modified { get; init; }

    [JsonPropertyName("original")]
    public PullRequestDiffBlockSide? Original { get; init; }
}

internal sealed class PullRequestDiffBlockSide
{
    [JsonPropertyName("startLine")]
    public int StartLine { get; init; }

    [JsonPropertyName("lineCount")]
    public int LineCount { get; init; }
}
