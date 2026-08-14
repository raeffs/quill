using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Quill.AzureDevOps.Dto;
using Quill.Core;
using Quill.Core.Models;

namespace Quill.AzureDevOps;

public class AzureDevOpsClient : IAzureDevOpsClient, IWorkItemBatchFetcher
{
    private const int BatchChunkSize = 200;

    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly string _batchUrl;
    private readonly string _collectionUrl;
    private readonly string _projectUrl;

    public AzureDevOpsClient(HttpClient httpClient, string serverUrl, string collection, string project)
    {
        _httpClient = httpClient;
        _baseUrl = $"{serverUrl.TrimEnd('/')}/{collection}/{project}/_apis/wit/workitems";
        _batchUrl = $"{serverUrl.TrimEnd('/')}/{collection}/{project}/_apis/wit/workitemsbatch";
        _collectionUrl = $"{serverUrl.TrimEnd('/')}/{collection}";
        _projectUrl = $"{serverUrl.TrimEnd('/')}/{collection}/{project}";
    }

    public async Task<BatchFetchResult> FetchAsync(IReadOnlyList<int> ids, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ids);
        if (ids.Count == 0)
        {
            return new BatchFetchResult
            {
                Items = Array.Empty<WorkItem>(),
                BatchFailedIds = Array.Empty<int>(),
            };
        }

        var allItems = new List<WorkItem>(ids.Count);
        var batchFailed = new List<int>();

        for (var offset = 0; offset < ids.Count; offset += BatchChunkSize)
        {
            var chunk = ids.Skip(offset).Take(BatchChunkSize).ToArray();
            var chunkItems = await FetchChunkAsync(chunk, cancellationToken);
            if (chunkItems is null)
            {
                batchFailed.AddRange(chunk);
            }
            else
            {
                allItems.AddRange(chunkItems);
            }
        }

        return new BatchFetchResult
        {
            Items = allItems,
            BatchFailedIds = batchFailed,
        };
    }

    public async Task<IReadOnlyList<int>> QueryByWiqlAsync(string wiql, int top, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(wiql);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(top);

        var url = $"{_projectUrl}/_apis/wit/wiql?$top={top}&api-version={AzureDevOpsConstants.ApiVersion}";
        var requestDto = new WiqlQueryRequest { Query = wiql };
        var json = JsonSerializer.Serialize(requestDto, AzureDevOpsJsonContext.Default.WiqlQueryRequest);

        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync(url, content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var dto = await response.Content.ReadFromJsonAsync(
            AzureDevOpsJsonContext.Default.WiqlQueryResponse, cancellationToken)
            ?? throw new InvalidOperationException("Failed to deserialize WIQL query response.");

        return dto.WorkItems.Select(w => w.Id).ToList();
    }

    public async Task<WorkItem> GetWorkItemAsync(int id)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);
        var url = $"{_baseUrl}/{id}?$expand=relations&api-version={AzureDevOpsConstants.ApiVersion}";
        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var dto = await response.Content.ReadFromJsonAsync(AzureDevOpsJsonContext.Default.WorkItemResponse)
            ?? throw new InvalidOperationException("Failed to deserialize work item response.");

        return MapToWorkItem(dto);
    }

    public async Task UpdateWorkItemFieldsAsync(int id, string type, string title, string descriptionHtml)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);
        var operations = new List<JsonPatchOperation>
        {
            new () { Op = "replace", Path = $"/fields/{AzureDevOpsConstants.FieldTitle}", Value = JsonValue.Create(title) },
            new () { Op = "replace", Path = $"/fields/{DescriptionFieldFor(type)}", Value = JsonValue.Create(descriptionHtml) },
        };

        await PatchWorkItemAsync(id, operations);
    }

    public async Task AddRelationAsync(int sourceId, int targetId)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sourceId);
        var operations = new List<JsonPatchOperation>
        {
            new ()
            {
                Op = "add",
                Path = "/relations/-",
                Value = JsonSerializer.SerializeToNode(
                    new RelationValue
                    {
                        Rel = AzureDevOpsConstants.RelatedLinkType,
                        Url = $"{_collectionUrl}/_apis/wit/workitems/{targetId}",
                    },
                    AzureDevOpsJsonContext.Default.RelationValue),
            },
        };

        await PatchWorkItemAsync(sourceId, operations);
    }

    public async Task<int> CreateWorkItemAsync(string type, string title, int parentId, string? assignedToId = null, string? descriptionHtml = null, string? iterationPath = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(parentId);
        var url = $"{_baseUrl}/${type}?api-version={AzureDevOpsConstants.ApiVersion}";

        var operations = new List<JsonPatchOperation>
        {
            new() { Op = "add", Path = $"/fields/{AzureDevOpsConstants.FieldTitle}", Value = JsonValue.Create(title) },
            new()
            {
                Op = "add",
                Path = "/relations/-",
                Value = JsonSerializer.SerializeToNode(
                    new RelationValue
                    {
                        Rel = AzureDevOpsConstants.HierarchyReverseLinkType,
                        Url = $"{_collectionUrl}/_apis/wit/workitems/{parentId}",
                    },
                    AzureDevOpsJsonContext.Default.RelationValue),
            },
        };

        if (assignedToId is not null)
        {
            operations.Add(new() { Op = "add", Path = $"/fields/{AzureDevOpsConstants.FieldAssignedTo}", Value = JsonValue.Create(assignedToId) });
        }

        if (!string.IsNullOrEmpty(descriptionHtml))
        {
            operations.Add(new() { Op = "add", Path = $"/fields/{DescriptionFieldFor(type)}", Value = JsonValue.Create(descriptionHtml) });
        }

        if (!string.IsNullOrEmpty(iterationPath))
        {
            operations.Add(new() { Op = "add", Path = $"/fields/{AzureDevOpsConstants.FieldIterationPath}", Value = JsonValue.Create(iterationPath) });
        }

        var json = JsonSerializer.Serialize(operations, AzureDevOpsJsonContext.Default.ListJsonPatchOperation);
        using var content = new StringContent(json, Encoding.UTF8, "application/json-patch+json");

        var response = await _httpClient.PatchAsync(url, content);
        response.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("id").GetInt32();
    }

    public async Task<IReadOnlyList<WorkItemComment>> GetCommentsAsync(int id, int? limit = null, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);
        if (limit is not null)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(limit.Value, nameof(limit));
        }

        const int pageSize = 200;
        var results = new List<WorkItemComment>();
        string? continuationToken = null;

        while (true)
        {
            var remaining = limit is null ? pageSize : Math.Min(pageSize, limit.Value - results.Count);
            var url = $"{_baseUrl}/{id}/comments?$top={remaining}&order=desc&api-version={AzureDevOpsConstants.ApiVersion}";
            if (!string.IsNullOrEmpty(continuationToken))
            {
                url += $"&continuationToken={Uri.EscapeDataString(continuationToken)}";
            }

            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var dto = await response.Content.ReadFromJsonAsync(
                AzureDevOpsJsonContext.Default.CommentsResponse, cancellationToken)
                ?? throw new InvalidOperationException("Failed to deserialize comments response.");

            foreach (var c in dto.Comments)
            {
                if (c.IsDeleted)
                {
                    continue;
                }

                results.Add(new WorkItemComment
                {
                    Id = c.Id,
                    Author = string.IsNullOrEmpty(c.CreatedBy?.DisplayName) ? null : c.CreatedBy.DisplayName,
                    CreatedDate = c.CreatedDate,
                    ModifiedDate = c.ModifiedDate,
                    TextHtml = c.Text,
                });

                if (limit is not null && results.Count >= limit.Value)
                {
                    return results;
                }
            }

            if (string.IsNullOrEmpty(dto.ContinuationToken))
            {
                return results;
            }

            continuationToken = dto.ContinuationToken;
        }
    }

    private static bool IsTransientStatus(HttpStatusCode status)
    {
        if (status == HttpStatusCode.TooManyRequests)
        {
            return true;
        }

        return (int)status >= 500 && (int)status < 600;
    }

    private static TimeSpan GetRetryAfter(HttpResponseMessage response)
    {
        var retryAfter = response.Headers.RetryAfter;
        if (retryAfter is null)
        {
            return TimeSpan.Zero;
        }

        if (retryAfter.Delta is TimeSpan delta)
        {
            return delta;
        }

        if (retryAfter.Date is DateTimeOffset date)
        {
            var diff = date - DateTimeOffset.UtcNow;
            return diff > TimeSpan.Zero ? diff : TimeSpan.Zero;
        }

        return TimeSpan.Zero;
    }

    private async Task PatchWorkItemAsync(int id, List<JsonPatchOperation> operations)
    {
        var url = $"{_baseUrl}/{id}?api-version={AzureDevOpsConstants.ApiVersion}";
        var json = JsonSerializer.Serialize(operations, AzureDevOpsJsonContext.Default.ListJsonPatchOperation);
        using var content = new StringContent(json, Encoding.UTF8, "application/json-patch+json");

        var response = await _httpClient.PatchAsync(url, content);
        response.EnsureSuccessStatusCode();
    }

    private async Task<List<WorkItem>?> FetchChunkAsync(IReadOnlyList<int> chunk, CancellationToken cancellationToken)
    {
        var first = await TrySendBatchAsync(chunk, cancellationToken);
        if (first.Items is not null)
        {
            return first.Items;
        }

        if (first.RetryAfter > TimeSpan.Zero)
        {
            await Task.Delay(first.RetryAfter, cancellationToken);
        }

        var second = await TrySendBatchAsync(chunk, cancellationToken);
        return second.Items;
    }

    // Returns null with an optional RetryAfter on transient failures (5xx / 429 / network / timeout).
    // Non-transient non-success statuses throw.
    private async Task<(List<WorkItem>? Items, TimeSpan RetryAfter)> TrySendBatchAsync(
        IReadOnlyList<int> chunk, CancellationToken cancellationToken)
    {
        var url = $"{_batchUrl}?api-version={AzureDevOpsConstants.ApiVersion}";
        var requestDto = new WorkItemsBatchRequest { Ids = chunk };
        var json = JsonSerializer.Serialize(requestDto, AzureDevOpsJsonContext.Default.WorkItemsBatchRequest);

        HttpResponseMessage response;
        try
        {
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            response = await _httpClient.PostAsync(url, content, cancellationToken);
        }
        catch (HttpRequestException)
        {
            return (null, TimeSpan.Zero);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return (null, TimeSpan.Zero);
        }

        using (response)
        {
            if (response.IsSuccessStatusCode)
            {
                var dto = await response.Content.ReadFromJsonAsync(
                    AzureDevOpsJsonContext.Default.WorkItemsBatchResponse, cancellationToken)
                    ?? throw new InvalidOperationException("Failed to deserialize work items batch response.");
                var items = new List<WorkItem>(dto.Value.Count);
                foreach (var itemDto in dto.Value)
                {
                    items.Add(MapToWorkItem(itemDto));
                }
                return (items, TimeSpan.Zero);
            }

            if (IsTransientStatus(response.StatusCode))
            {
                return (null, GetRetryAfter(response));
            }

            response.EnsureSuccessStatusCode();
            throw new InvalidOperationException($"Unexpected batch response: {response.StatusCode}");
        }
    }

    private static WorkItem MapToWorkItem(WorkItemResponse dto)
    {
        var fields = dto.Fields;

        var assignedToId = string.Empty;
        var assignedToDisplayName = string.Empty;
        if (fields.TryGetValue(AzureDevOpsConstants.FieldAssignedTo, out var assignedToEl))
        {
            if (assignedToEl.ValueKind == JsonValueKind.Object)
            {
                if (assignedToEl.TryGetProperty("id", out var id))
                {
                    assignedToId = id.GetString() ?? string.Empty;
                }
                if (assignedToEl.TryGetProperty("displayName", out var dn))
                {
                    assignedToDisplayName = dn.GetString() ?? string.Empty;
                }
            }
            else if (assignedToEl.ValueKind == JsonValueKind.String)
            {
                assignedToId = assignedToEl.GetString() ?? string.Empty;
            }
        }

        var relations = new List<WorkItemRelation>();
        if (dto.Relations is not null)
        {
            foreach (var rel in dto.Relations)
            {
                if (string.Equals(rel.Rel, AzureDevOpsConstants.RelatedLinkType, StringComparison.Ordinal))
                {
                    var urlParts = rel.Url.Split('/');
                    if (int.TryParse(urlParts[^1], System.Globalization.CultureInfo.InvariantCulture, out var targetId))
                    {
                        relations.Add(new WorkItemRelation
                        {
                            RelationType = rel.Rel,
                            TargetId = targetId,
                        });
                    }
                }
            }
        }

        int? parentId = null;
        var childIds = new List<int>();
        if (dto.Relations is not null)
        {
            foreach (var rel in dto.Relations)
            {
                if (string.Equals(rel.Rel, AzureDevOpsConstants.HierarchyReverseLinkType, StringComparison.Ordinal))
                {
                    var urlParts = rel.Url.Split('/');
                    if (int.TryParse(urlParts[^1], System.Globalization.CultureInfo.InvariantCulture, out var pid))
                    {
                        parentId = pid;
                    }
                }
                else if (string.Equals(rel.Rel, AzureDevOpsConstants.HierarchyForwardLinkType, StringComparison.Ordinal))
                {
                    var urlParts = rel.Url.Split('/');
                    if (int.TryParse(urlParts[^1], System.Globalization.CultureInfo.InvariantCulture, out var childId))
                    {
                        childIds.Add(childId);
                    }
                }
            }
        }

        var type = GetStringField(fields, AzureDevOpsConstants.FieldWorkItemType);

        return new WorkItem
        {
            Id = dto.Id,
            Type = type,
            Title = GetStringField(fields, AzureDevOpsConstants.FieldTitle),
            State = GetStringField(fields, AzureDevOpsConstants.FieldState),
            AssignedToId = assignedToId,
            AssignedToDisplayName = assignedToDisplayName,
            Description = GetStringField(fields, DescriptionFieldFor(type)),
            IterationPath = GetStringField(fields, AzureDevOpsConstants.FieldIterationPath),
            ParentId = parentId,
            Relations = relations,
            ChildIds = childIds,
        };
    }

    private static string DescriptionFieldFor(string workItemType) =>
        string.Equals(workItemType, "Bug", StringComparison.Ordinal)
            ? AzureDevOpsConstants.FieldReproSteps
            : AzureDevOpsConstants.FieldDescription;

    private static string GetStringField(IReadOnlyDictionary<string, JsonElement> fields, string key)
    {
        if (fields.TryGetValue(key, out var element) && element.ValueKind == JsonValueKind.String)
        {
            return element.GetString() ?? string.Empty;
        }

        return string.Empty;
    }
}
