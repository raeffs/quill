using System.Globalization;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Quill.AzureDevOps.Dto;
using Quill.Core;
using Quill.Core.Models;

namespace Quill.AzureDevOps;

public class AzureDevOpsPullRequestClient : IAzureDevOpsPullRequestClient
{
    private const string RefsHeadsPrefix = "refs/heads/";
    private const string CodeReviewThreadTypeKey = "CodeReviewThreadType";

    // Azure DevOps writes this on an end offset to mean "to the end of the line"
    private const int EndOfLineOffset = 2147483647;

    private static readonly HashSet<string> BinaryExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".gif", ".ico", ".webp", ".bmp", ".tiff", ".svg",
        ".pdf", ".zip", ".tar", ".gz", ".7z", ".rar",
        ".exe", ".dll", ".so", ".dylib", ".bin", ".class", ".jar",
        ".ttf", ".otf", ".woff", ".woff2", ".eot",
        ".mp3", ".mp4", ".wav", ".avi", ".mov", ".mkv",
        ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx",
    };

    private readonly Dictionary<string, IReadOnlyList<PullRequestIterationResponse>> _iterationsByPullRequest =
        new(StringComparer.Ordinal);

    private readonly HttpClient _httpClient;
    private readonly string _serverUrl;
    private readonly string _collection;
    private readonly string _project;

    public AzureDevOpsPullRequestClient(HttpClient httpClient, string serverUrl, string collection, string project)
    {
        _httpClient = httpClient;
        _serverUrl = serverUrl.TrimEnd('/');
        _collection = collection;
        _project = project;
    }

    public async Task<IReadOnlyList<PullRequest>> ListAsync(
        PullRequestListQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentException.ThrowIfNullOrWhiteSpace(query.Status, nameof(query));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(query.Top, nameof(query));

        if (query.Skip is { } skip)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(skip, nameof(query));
        }

        var baseUrl = string.IsNullOrEmpty(query.Repo)
            ? $"{_serverUrl}/{_collection}/{_project}/_apis/git/pullrequests"
            : $"{_serverUrl}/{_collection}/{_project}/_apis/git/repositories/{Uri.EscapeDataString(query.Repo)}/pullrequests";

        var parameters = new List<string>
        {
            $"searchCriteria.status={Uri.EscapeDataString(query.Status)}",
            $"$top={query.Top.ToString(CultureInfo.InvariantCulture)}",
            $"api-version={AzureDevOpsConstants.ApiVersion}",
        };

        if (!string.IsNullOrEmpty(query.CreatorId))
        {
            parameters.Add($"searchCriteria.creatorId={Uri.EscapeDataString(query.CreatorId)}");
        }

        if (!string.IsNullOrEmpty(query.ReviewerId))
        {
            parameters.Add($"searchCriteria.reviewerId={Uri.EscapeDataString(query.ReviewerId)}");
        }

        if (!string.IsNullOrEmpty(query.SourceBranch))
        {
            parameters.Add($"searchCriteria.sourceRefName={Uri.EscapeDataString(ToRefName(query.SourceBranch))}");
        }

        if (!string.IsNullOrEmpty(query.TargetBranch))
        {
            parameters.Add($"searchCriteria.targetRefName={Uri.EscapeDataString(ToRefName(query.TargetBranch))}");
        }

        if (query.Skip is { } skipValue)
        {
            parameters.Add($"$skip={skipValue.ToString(CultureInfo.InvariantCulture)}");
        }

        var url = $"{baseUrl}?{string.Join('&', parameters)}";

        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var dto = await response.Content.ReadFromJsonAsync(
            AzureDevOpsJsonContext.Default.PullRequestListResponse, cancellationToken)
            ?? throw new InvalidOperationException("Failed to deserialize pull request list response.");

        var results = new List<PullRequest>(dto.Value.Count);
        foreach (var item in dto.Value)
        {
            results.Add(MapToPullRequest(item));
        }

        return results;
    }

    public async Task<PullRequest> GetByIdAsync(int prId, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(prId);

        var url = $"{_serverUrl}/{_collection}/{_project}/_apis/git/pullrequests/{prId.ToString(CultureInfo.InvariantCulture)}?api-version={AzureDevOpsConstants.ApiVersion}";

        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var dto = await response.Content.ReadFromJsonAsync(
            AzureDevOpsJsonContext.Default.PullRequestItemResponse, cancellationToken)
            ?? throw new InvalidOperationException("Failed to deserialize pull request response.");

        return MapToPullRequest(dto);
    }

    public async Task<IReadOnlyList<PullRequestThread>> GetThreadsAsync(
        int prId,
        string repo,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(prId);
        ArgumentException.ThrowIfNullOrWhiteSpace(repo);

        var iterations = await GetIterationsAsync(prId, repo, cancellationToken);
        var latestIteration = LatestIteration(iterations)?.Id;

        // Both parameters are required. `$iteration` alone is ignored, and `$baseIteration=0`
        // normalises to the whole-pull-request diff, which is what re-tracks the anchors. An empty
        // pull request has no iteration to scope to, so it falls back to the plain call. See ADR 0006.
        var scope = latestIteration is { } n
            ? $"$iteration={n.ToString(CultureInfo.InvariantCulture)}&$baseIteration=0&"
            : string.Empty;
        var url = $"{PullRequestRoot(prId, repo)}/threads?{scope}api-version={AzureDevOpsConstants.ApiVersion}";

        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var dto = await response.Content.ReadFromJsonAsync(
            AzureDevOpsJsonContext.Default.PullRequestThreadsResponse, cancellationToken)
            ?? throw new InvalidOperationException("Failed to deserialize pull request threads response.");

        var results = new List<PullRequestThread>(dto.Value.Count);
        foreach (var thread in dto.Value)
        {
            if (thread.IsDeleted)
            {
                continue;
            }

            if (IsSystemThread(thread.Properties))
            {
                continue;
            }

            var comments = thread.Comments ?? Array.Empty<PullRequestThreadCommentResponse>();
            var liveComments = new List<PullRequestComment>(comments.Count);
            foreach (var c in comments)
            {
                if (c.IsDeleted || c.Content is null)
                {
                    continue;
                }

                var modified = c.LastContentUpdatedDate is { } m && m != c.PublishedDate ? (DateTimeOffset?)m : null;
                liveComments.Add(new PullRequestComment
                {
                    Id = c.Id,
                    Author = string.IsNullOrEmpty(c.Author?.DisplayName) ? null : c.Author.DisplayName,
                    CreatedDate = c.PublishedDate,
                    ModifiedDate = modified,
                    LastUpdatedDate = c.LastUpdatedDate ?? c.PublishedDate,
                    UsersLiked = ToDisplayNames(c.UsersLiked),
                    TextHtml = c.Content,
                });
            }

            if (liveComments.Count == 0)
            {
                continue;
            }

            liveComments.Sort((a, b) => a.CreatedDate.CompareTo(b.CreatedDate));

            var position = ResolveThreadPosition(thread, latestIteration);

            results.Add(new PullRequestThread
            {
                Id = thread.Id,
                Status = thread.Status ?? string.Empty,
                PublishedDate = thread.PublishedDate,
                LastUpdatedDate = thread.LastUpdatedDate ?? thread.PublishedDate,
                FilePath = position.FilePath,
                Side = position.Side,
                StartLine = position.StartLine,
                EndLine = position.EndLine,
                PositionState = position.State,
                OrigFilePath = position.OrigFilePath,
                OrigStartLine = position.OrigStartLine,
                OrigEndLine = position.OrigEndLine,
                OrigStartColumn = position.OrigStartColumn,
                OrigEndColumn = position.OrigEndColumn,
                Comments = liveComments,
            });
        }

        results.Sort((a, b) => b.PublishedDate.CompareTo(a.PublishedDate));
        return results;
    }

    public async Task<IReadOnlyList<int>> GetWorkItemRefsAsync(
        int prId,
        string repo,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(prId);
        ArgumentException.ThrowIfNullOrWhiteSpace(repo);

        var url = $"{_serverUrl}/{_collection}/{_project}/_apis/git/repositories/{Uri.EscapeDataString(repo)}/pullRequests/{prId.ToString(CultureInfo.InvariantCulture)}/workitems?api-version={AzureDevOpsConstants.ApiVersion}";

        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var dto = await response.Content.ReadFromJsonAsync(
            AzureDevOpsJsonContext.Default.PullRequestWorkItemRefsResponse, cancellationToken)
            ?? throw new InvalidOperationException("Failed to deserialize pull request work item refs response.");

        var ids = new List<int>(dto.Value.Count);
        foreach (var item in dto.Value)
        {
            if (int.TryParse(item.Id, CultureInfo.InvariantCulture, out var parsed))
            {
                ids.Add(parsed);
            }
        }

        return ids;
    }

    public async Task<PullRequestDiffStats> GetDiffStatsAsync(
        int prId,
        string repo,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(prId);
        ArgumentException.ThrowIfNullOrWhiteSpace(repo);

        var prRoot = PullRequestRoot(prId, repo);

        var iterations = await GetIterationsAsync(prId, repo, cancellationToken);
        if (LatestIteration(iterations) is not { } latest)
        {
            return EmptyDiffStats();
        }

        var iterationId = latest.Id;
        var baseCommit = latest.CommonRefCommit?.CommitId;
        var targetCommit = latest.SourceRefCommit?.CommitId;

        var changesUrl = $"{prRoot}/iterations/{iterationId.ToString(CultureInfo.InvariantCulture)}/changes?$top=10000&api-version={AzureDevOpsConstants.ApiVersion}";
        var changesResponse = await _httpClient.GetAsync(changesUrl, cancellationToken);
        changesResponse.EnsureSuccessStatusCode();
        var changesDto = await changesResponse.Content.ReadFromJsonAsync(
            AzureDevOpsJsonContext.Default.PullRequestIterationChangesResponse, cancellationToken)
            ?? throw new InvalidOperationException("Failed to deserialize pull request iteration changes response.");

        var fileChanges = new List<(string Path, string ChangeType, string? OriginalPath)>();
        foreach (var entry in changesDto.ChangeEntries)
        {
            if (entry.Item is null || entry.Item.IsFolder || string.IsNullOrEmpty(entry.Item.Path))
            {
                continue;
            }

            fileChanges.Add((entry.Item.Path, entry.ChangeType, entry.OriginalPath));
        }

        if (fileChanges.Count == 0)
        {
            return EmptyDiffStats();
        }

        var lineCountsByPath = new Dictionary<string, (int Added, int Removed)>(StringComparer.Ordinal);
        var binaryByPath = new HashSet<string>(StringComparer.Ordinal);

        if (!string.IsNullOrEmpty(baseCommit) && !string.IsNullOrEmpty(targetCommit))
        {
            var diffParams = new List<PullRequestFileDiffParam>(fileChanges.Count);
            foreach (var change in fileChanges)
            {
                if (IsAddChange(change.ChangeType))
                {
                    diffParams.Add(new PullRequestFileDiffParam { Path = change.Path, OriginalPath = string.Empty });
                }
                else if (IsDeleteChange(change.ChangeType))
                {
                    diffParams.Add(new PullRequestFileDiffParam { Path = string.Empty, OriginalPath = change.OriginalPath ?? change.Path });
                }
                else
                {
                    diffParams.Add(new PullRequestFileDiffParam { Path = change.Path, OriginalPath = change.OriginalPath ?? change.Path });
                }
            }

            var fileDiffsUrl = $"{prRoot}/fileDiffs?baseVersionCommit={Uri.EscapeDataString(baseCommit)}&targetVersionCommit={Uri.EscapeDataString(targetCommit)}&api-version={AzureDevOpsConstants.ApiVersion}";
            var requestDto = new PullRequestFileDiffsRequest { FileDiffParams = diffParams };
            var requestJson = JsonSerializer.Serialize(requestDto, AzureDevOpsJsonContext.Default.PullRequestFileDiffsRequest);
            using var requestContent = new StringContent(requestJson, Encoding.UTF8, "application/json");

            var diffsResponse = await _httpClient.PostAsync(fileDiffsUrl, requestContent, cancellationToken);
            diffsResponse.EnsureSuccessStatusCode();

            var diffEntries = await diffsResponse.Content.ReadFromJsonAsync(
                AzureDevOpsJsonContext.Default.ListPullRequestFileDiffEntry, cancellationToken);

            if (diffEntries is not null)
            {
                foreach (var entry in diffEntries)
                {
                    var key = string.IsNullOrEmpty(entry.Path) ? entry.OriginalPath : entry.Path;
                    if (string.IsNullOrEmpty(key))
                    {
                        continue;
                    }

                    if (entry.BinaryContent)
                    {
                        binaryByPath.Add(key);
                        continue;
                    }

                    int added = 0;
                    int removed = 0;
                    foreach (var block in entry.LineCharBlocks)
                    {
                        if (block.Modified is { LineCount: > 0 } modified)
                        {
                            added += modified.LineCount;
                        }

                        if (block.Original is { LineCount: > 0 } original)
                        {
                            removed += original.LineCount;
                        }
                    }

                    lineCountsByPath[key] = (added, removed);
                }
            }
        }

        var files = new List<PullRequestDiffFile>(fileChanges.Count);
        int totalAdded = 0;
        int totalRemoved = 0;
        foreach (var change in fileChanges)
        {
            var changeType = NormalizeChangeType(change.ChangeType);
            var displayPath = StripLeadingSlash(change.Path);
            var oldDisplayPath = change.OriginalPath is null ? null : StripLeadingSlash(change.OriginalPath);
            var isBinary = binaryByPath.Contains(change.Path) || HasBinaryExtension(displayPath);

            var isRename = string.Equals(changeType, "rename", StringComparison.Ordinal);

            int added;
            int removed;
            if (isBinary || isRename)
            {
                added = 0;
                removed = 0;
            }
            else if (lineCountsByPath.TryGetValue(change.Path, out var counts))
            {
                added = counts.Added;
                removed = counts.Removed;
            }
            else
            {
                added = 0;
                removed = 0;
            }

            files.Add(new PullRequestDiffFile
            {
                Path = displayPath,
                ChangeType = changeType,
                OldPath = isRename ? oldDisplayPath : null,
                Added = added,
                Removed = removed,
                Binary = isBinary,
            });

            totalAdded += added;
            totalRemoved += removed;
        }

        return new PullRequestDiffStats
        {
            TotalFiles = files.Count,
            TotalAdded = totalAdded,
            TotalRemoved = totalRemoved,
            Files = files,
        };
    }

    private static PullRequestDiffStats EmptyDiffStats() => new()
    {
        TotalFiles = 0,
        TotalAdded = 0,
        TotalRemoved = 0,
        Files = Array.Empty<PullRequestDiffFile>(),
    };

    private static string NormalizeChangeType(string adoChangeType)
    {
        if (string.IsNullOrEmpty(adoChangeType))
        {
            return "edit";
        }

        var tokens = adoChangeType.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var set = new HashSet<string>(tokens, StringComparer.OrdinalIgnoreCase);

        if (set.Contains("rename"))
        {
            return "rename";
        }

        if (set.Contains("delete"))
        {
            return "delete";
        }

        if (set.Contains("add"))
        {
            return "add";
        }

        return "edit";
    }

    private static bool IsAddChange(string adoChangeType)
    {
        return Tokenize(adoChangeType).Contains("add");
    }

    private static bool IsDeleteChange(string adoChangeType)
    {
        var tokens = Tokenize(adoChangeType);
        return tokens.Contains("delete") && !tokens.Contains("rename");
    }

    private static HashSet<string> Tokenize(string adoChangeType)
    {
        var tokens = adoChangeType.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return new HashSet<string>(tokens, StringComparer.OrdinalIgnoreCase);
    }

    private static string StripLeadingSlash(string path)
    {
        return path.StartsWith('/') ? path[1..] : path;
    }

    private static bool HasBinaryExtension(string path)
    {
        var ext = Path.GetExtension(path);
        if (string.IsNullOrEmpty(ext))
        {
            return false;
        }

        return BinaryExtensions.Contains(ext);
    }

    private static bool IsSystemThread(JsonElement? properties)
    {
        if (properties is null || properties.Value.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        return properties.Value.TryGetProperty(CodeReviewThreadTypeKey, out _);
    }

    private string PullRequestRoot(int prId, string repo) =>
        $"{_serverUrl}/{_collection}/{_project}/_apis/git/repositories/{Uri.EscapeDataString(repo)}/pullRequests/{prId.ToString(CultureInfo.InvariantCulture)}";

    private async Task<IReadOnlyList<PullRequestIterationResponse>> GetIterationsAsync(
        int prId,
        string repo,
        CancellationToken cancellationToken)
    {
        var key = $"{prId.ToString(CultureInfo.InvariantCulture)}/{repo}";
        if (_iterationsByPullRequest.TryGetValue(key, out var cached))
        {
            return cached;
        }

        var url = $"{PullRequestRoot(prId, repo)}/iterations?api-version={AzureDevOpsConstants.ApiVersion}";
        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var dto = await response.Content.ReadFromJsonAsync(
            AzureDevOpsJsonContext.Default.PullRequestIterationsResponse, cancellationToken)
            ?? throw new InvalidOperationException("Failed to deserialize pull request iterations response.");

        _iterationsByPullRequest[key] = dto.Value;
        return dto.Value;
    }

    private static PullRequestIterationResponse? LatestIteration(IReadOnlyList<PullRequestIterationResponse> iterations)
    {
        PullRequestIterationResponse? latest = null;
        foreach (var iteration in iterations)
        {
            if (latest is null || iteration.Id > latest.Id)
            {
                latest = iteration;
            }
        }

        return latest;
    }

    private static ThreadPosition ResolveThreadPosition(PullRequestThreadResponse thread, int? latestIteration)
    {
        var context = thread.ThreadContext;
        if (context is null || string.IsNullOrEmpty(context.FilePath))
        {
            return new ThreadPosition();
        }

        var filePath = StripLeadingSlash(context.FilePath);
        var (side, start, end) = ResolveAnchor(context);

        // Azure DevOps re-tracks the anchor and moves the reviewer's own position into the tracking
        // criteria. Without them, the two positions are the same one.
        var criteria = thread.PullRequestThreadContext?.TrackingCriteria;
        var (trackedStart, trackedEnd) = ResolveTrackingAnchor(criteria);
        var origStart = trackedStart ?? start;
        var origEnd = trackedEnd ?? end;

        var origFilePath = criteria?.OrigFilePath is { Length: > 0 } tracked
            ? StripLeadingSlash(tracked)
            : null;

        return new ThreadPosition
        {
            FilePath = filePath,
            Side = side,
            StartLine = start?.Line,
            EndLine = end?.Line,
            State = DerivePositionState(thread.PullRequestThreadContext, latestIteration, trackedStart, trackedEnd),
            OrigFilePath = string.Equals(origFilePath, filePath, StringComparison.Ordinal) ? null : origFilePath,
            OrigStartLine = origStart?.Line,
            OrigEndLine = origEnd?.Line,
            OrigStartColumn = ToColumn(origStart?.Offset),
            OrigEndColumn = ToColumn(origEnd?.Offset),
        };
    }

    private static (string? Side, PullRequestFilePosition? Start, PullRequestFilePosition? End) ResolveAnchor(
        PullRequestThreadContextResponse context)
    {
        if (context.RightFileStart is { } rs)
        {
            return ("right", rs, context.RightFileEnd ?? rs);
        }

        if (context.LeftFileStart is { } ls)
        {
            return ("left", ls, context.LeftFileEnd ?? ls);
        }

        return (null, null, null);
    }

    private static (PullRequestFilePosition? Start, PullRequestFilePosition? End) ResolveTrackingAnchor(
        PullRequestTrackingCriteriaResponse? criteria)
    {
        if (criteria?.OrigRightFileStart is { } rs)
        {
            return (rs, criteria.OrigRightFileEnd ?? rs);
        }

        if (criteria?.OrigLeftFileStart is { } ls)
        {
            return (ls, criteria.OrigLeftFileEnd ?? ls);
        }

        return (null, null);
    }

    private static string DerivePositionState(
        PullRequestIterationThreadContextResponse? context,
        int? latestIteration,
        PullRequestFilePosition? trackedStart,
        PullRequestFilePosition? trackedEnd)
    {
        // The reviewer commented on the latest iteration, so nothing can have moved.
        if (latestIteration is { } n && context?.IterationContext?.FirstComparingIteration == n)
        {
            return "current";
        }

        // Azure DevOps widened the anchor to whole lines, which it does when the code survives.
        if (trackedEnd?.Offset == EndOfLineOffset)
        {
            return "tracked";
        }

        // A zero-width caret marks the join point where the code was.
        if (trackedStart is { Offset: 1 } start && trackedEnd is { Offset: 1 } end && start.Line == end.Line)
        {
            return "deleted";
        }

        return "unverified";
    }

    private static int? ToColumn(int? offset)
    {
        return offset is > 0 and not EndOfLineOffset ? offset : null;
    }

    private static IReadOnlyList<string> ToDisplayNames(IReadOnlyList<PullRequestThreadAuthorResponse>? identities)
    {
        if (identities is null || identities.Count == 0)
        {
            return Array.Empty<string>();
        }

        var names = new List<string>(identities.Count);
        foreach (var identity in identities)
        {
            if (!string.IsNullOrEmpty(identity.DisplayName))
            {
                names.Add(identity.DisplayName);
            }
        }

        return names;
    }

    private PullRequest MapToPullRequest(PullRequestItemResponse dto)
    {
        var repoName = dto.Repository?.Name
            ?? throw new InvalidOperationException($"Pull request {dto.PullRequestId.ToString(CultureInfo.InvariantCulture)} has no repository.");
        var webUrl = $"{_serverUrl}/{_collection}/{_project}/_git/{repoName}/pullrequest/{dto.PullRequestId.ToString(CultureInfo.InvariantCulture)}";

        var dtoReviewers = dto.Reviewers ?? [];
        var reviewers = new List<PullRequestReviewer>(dtoReviewers.Count);
        foreach (var r in dtoReviewers)
        {
            reviewers.Add(new PullRequestReviewer
            {
                Id = r.Id,
                DisplayName = r.DisplayName,
                Vote = r.Vote,
                IsRequired = r.IsRequired,
                IsContainer = r.IsContainer,
            });
        }

        var dtoLabels = dto.Labels ?? [];
        var labels = new List<string>(dtoLabels.Count);
        foreach (var label in dtoLabels)
        {
            if (label.Active)
            {
                labels.Add(label.Name);
            }
        }

        return new PullRequest
        {
            Id = dto.PullRequestId,
            Title = dto.Title,
            AuthorDisplayName = dto.CreatedBy?.DisplayName ?? string.Empty,
            Status = dto.Status,
            IsDraft = dto.IsDraft,
            RepoName = repoName,
            SourceBranch = StripRefsHeads(dto.SourceRefName),
            TargetBranch = StripRefsHeads(dto.TargetRefName),
            CreatedDate = dto.CreationDate,
            ClosedDate = dto.ClosedDate,
            Reviewers = reviewers,
            MergeStatus = dto.MergeStatus,
            LastMergeSourceCommit = ToCommitId(dto.LastMergeSourceCommit),
            LastMergeTargetCommit = ToCommitId(dto.LastMergeTargetCommit),
            Labels = labels,
            WebUrl = webUrl,
            Description = dto.Description ?? string.Empty,
        };
    }

    private static string? ToCommitId(PullRequestCommitRefResponse? commitRef)
    {
        return string.IsNullOrEmpty(commitRef?.CommitId) ? null : commitRef.CommitId;
    }

    private static string StripRefsHeads(string refName)
    {
        return refName.StartsWith(RefsHeadsPrefix, StringComparison.Ordinal)
            ? refName.Substring(RefsHeadsPrefix.Length)
            : refName;
    }

    private static string ToRefName(string branch)
    {
        return branch.StartsWith("refs/", StringComparison.Ordinal)
            ? branch
            : RefsHeadsPrefix + branch;
    }

    private sealed class ThreadPosition
    {
        public string? FilePath { get; init; }

        public string? Side { get; init; }

        public int? StartLine { get; init; }

        public int? EndLine { get; init; }

        public string? State { get; init; }

        public string? OrigFilePath { get; init; }

        public int? OrigStartLine { get; init; }

        public int? OrigEndLine { get; init; }

        public int? OrigStartColumn { get; init; }

        public int? OrigEndColumn { get; init; }
    }
}
