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

    private static readonly HashSet<string> BinaryExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".gif", ".ico", ".webp", ".bmp", ".tiff", ".svg",
        ".pdf", ".zip", ".tar", ".gz", ".7z", ".rar",
        ".exe", ".dll", ".so", ".dylib", ".bin", ".class", ".jar",
        ".ttf", ".otf", ".woff", ".woff2", ".eot",
        ".mp3", ".mp4", ".wav", ".avi", ".mov", ".mkv",
        ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx",
    };

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

        var url = $"{_serverUrl}/{_collection}/{_project}/_apis/git/repositories/{Uri.EscapeDataString(repo)}/pullRequests/{prId.ToString(CultureInfo.InvariantCulture)}/threads?api-version={AzureDevOpsConstants.ApiVersion}";

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

            var liveComments = new List<WorkItemComment>(thread.Comments.Count);
            foreach (var c in thread.Comments)
            {
                if (c.IsDeleted)
                {
                    continue;
                }

                var modified = c.LastContentUpdatedDate is { } m && m != c.PublishedDate ? (DateTimeOffset?)m : null;
                liveComments.Add(new WorkItemComment
                {
                    Id = c.Id,
                    Author = string.IsNullOrEmpty(c.Author?.DisplayName) ? null : c.Author.DisplayName,
                    CreatedDate = c.PublishedDate,
                    ModifiedDate = modified,
                    TextHtml = c.Content,
                });
            }

            if (liveComments.Count == 0)
            {
                continue;
            }

            liveComments.Sort((a, b) => a.CreatedDate.CompareTo(b.CreatedDate));

            var (filePath, side, startLine, endLine) = ResolveThreadLocation(thread.ThreadContext);

            results.Add(new PullRequestThread
            {
                Id = thread.Id,
                Status = thread.Status ?? string.Empty,
                PublishedDate = thread.PublishedDate,
                FilePath = filePath,
                Side = side,
                StartLine = startLine,
                EndLine = endLine,
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

        var prRoot = $"{_serverUrl}/{_collection}/{_project}/_apis/git/repositories/{Uri.EscapeDataString(repo)}/pullRequests/{prId.ToString(CultureInfo.InvariantCulture)}";

        var iterationsUrl = $"{prRoot}/iterations?api-version={AzureDevOpsConstants.ApiVersion}";
        var iterationsResponse = await _httpClient.GetAsync(iterationsUrl, cancellationToken);
        iterationsResponse.EnsureSuccessStatusCode();
        var iterationsDto = await iterationsResponse.Content.ReadFromJsonAsync(
            AzureDevOpsJsonContext.Default.PullRequestIterationsResponse, cancellationToken)
            ?? throw new InvalidOperationException("Failed to deserialize pull request iterations response.");

        if (iterationsDto.Value.Count == 0)
        {
            return EmptyDiffStats();
        }

        var latest = iterationsDto.Value[^1];
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

    private static (string? FilePath, string? Side, int? StartLine, int? EndLine) ResolveThreadLocation(
        PullRequestThreadContextResponse? context)
    {
        if (context is null || string.IsNullOrEmpty(context.FilePath))
        {
            return (null, null, null, null);
        }

        var filePath = context.FilePath.StartsWith('/') ? context.FilePath[1..] : context.FilePath;

        if (context.RightFileStart is { } rs)
        {
            return (filePath, "right", rs.Line, context.RightFileEnd?.Line ?? rs.Line);
        }

        if (context.LeftFileStart is { } ls)
        {
            return (filePath, "left", ls.Line, context.LeftFileEnd?.Line ?? ls.Line);
        }

        return (filePath, null, null, null);
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
            Labels = labels,
            WebUrl = webUrl,
            Description = dto.Description ?? string.Empty,
        };
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
}
