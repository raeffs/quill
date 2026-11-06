using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Quill.AzureDevOps.Dto;
using Quill.Core;
using Quill.Core.Models;

namespace Quill.AzureDevOps;

public class AzureDevOpsBuildClient : IAzureDevOpsBuildClient
{
    private readonly HttpClient _httpClient;
    private readonly string _serverUrl;
    private readonly string _collection;
    private readonly string _project;

    public AzureDevOpsBuildClient(HttpClient httpClient, string serverUrl, string collection, string project)
    {
        _httpClient = httpClient;
        _serverUrl = serverUrl.TrimEnd('/');
        _collection = collection;
        _project = project;
    }

    public async Task<PipelineRun> GetRunAsync(int runId, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(runId);

        var url = $"{_serverUrl}/{_collection}/{_project}/_apis/build/builds/{runId.ToString(CultureInfo.InvariantCulture)}?api-version={AzureDevOpsConstants.ApiVersion}";

        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var dto = await response.Content.ReadFromJsonAsync(
            AzureDevOpsJsonContext.Default.BuildResponse, cancellationToken)
            ?? throw new InvalidOperationException("Failed to deserialize build response.");

        return ToPipelineRun(dto);
    }

    public async Task<IReadOnlyList<TimelineRecord>> GetTimelineAsync(int runId, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(runId);

        var url = $"{_serverUrl}/{_collection}/{_project}/_apis/build/builds/{runId.ToString(CultureInfo.InvariantCulture)}/timeline?api-version={AzureDevOpsConstants.ApiVersion}";

        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        if (response.StatusCode == HttpStatusCode.NoContent || response.Content.Headers.ContentLength == 0)
        {
            return [];
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(body))
        {
            return [];
        }

        var dto = JsonSerializer.Deserialize(body, AzureDevOpsJsonContext.Default.TimelineResponse)
            ?? throw new InvalidOperationException("Failed to deserialize timeline response.");

        return dto.Records.Select(ToTimelineRecord).ToList();
    }

    public async Task<IReadOnlyList<string>> GetLogLinesAsync(
        int runId,
        int logId,
        int? startLine,
        int? endLine,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(runId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(logId);

        var url = $"{_serverUrl}/{_collection}/{_project}/_apis/build/builds/{runId.ToString(CultureInfo.InvariantCulture)}/logs/{logId.ToString(CultureInfo.InvariantCulture)}?api-version={AzureDevOpsConstants.ApiVersion}";

        if (startLine is { } start)
        {
            url += $"&startLine={start.ToString(CultureInfo.InvariantCulture)}";
        }

        if (endLine is { } end)
        {
            url += $"&endLine={end.ToString(CultureInfo.InvariantCulture)}";
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        if (response.StatusCode == HttpStatusCode.NoContent || response.Content.Headers.ContentLength == 0)
        {
            return [];
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(body))
        {
            return [];
        }

        var dto = JsonSerializer.Deserialize(body, AzureDevOpsJsonContext.Default.BuildLogLinesResponse)
            ?? throw new InvalidOperationException("Failed to deserialize build log response.");

        return dto.Value.ToList();
    }

    public async Task<int> GetLogLineCountAsync(int runId, int logId, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(runId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(logId);

        var url = $"{_serverUrl}/{_collection}/{_project}/_apis/build/builds/{runId.ToString(CultureInfo.InvariantCulture)}/logs?api-version={AzureDevOpsConstants.ApiVersion}";

        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var dto = await response.Content.ReadFromJsonAsync(
            AzureDevOpsJsonContext.Default.BuildLogListResponse, cancellationToken)
            ?? throw new InvalidOperationException("Failed to deserialize build log list response.");

        var log = dto.Value.FirstOrDefault(entry => entry.Id == logId)
            ?? throw new HttpRequestException($"Log {logId} not found in run {runId}.", inner: null, HttpStatusCode.NotFound);

        return log.LineCount;
    }

    public async Task<IReadOnlyList<PipelineRun>> ListRunsAsync(
        PipelineRunListQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(query.Top, nameof(query));

        var parameters = new List<string>
        {
            $"$top={query.Top.ToString(CultureInfo.InvariantCulture)}",
            $"api-version={AzureDevOpsConstants.ApiVersion}",
            "queryOrder=queueTimeDescending",
        };

        if (query.PipelineId is { } pipelineId)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pipelineId, nameof(query));
            parameters.Add($"definitions={pipelineId.ToString(CultureInfo.InvariantCulture)}");
        }

        if (!string.IsNullOrEmpty(query.Branch))
        {
            parameters.Add($"branchName={Uri.EscapeDataString(RefNames.FromBranch(query.Branch))}");
        }

        if (!string.IsNullOrEmpty(query.Status))
        {
            parameters.Add($"statusFilter={Uri.EscapeDataString(query.Status)}");
        }

        if (!string.IsNullOrEmpty(query.Result))
        {
            parameters.Add($"resultFilter={Uri.EscapeDataString(query.Result)}");
        }

        if (!string.IsNullOrEmpty(query.Reason))
        {
            parameters.Add($"reasonFilter={Uri.EscapeDataString(query.Reason)}");
        }

        var url = $"{_serverUrl}/{_collection}/{_project}/_apis/build/builds?{string.Join('&', parameters)}";

        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var dto = await response.Content.ReadFromJsonAsync(
            AzureDevOpsJsonContext.Default.BuildListResponse, cancellationToken)
            ?? throw new InvalidOperationException("Failed to deserialize build list response.");

        return dto.Value.Select(ToPipelineRun).ToList();
    }

    public async Task<IReadOnlyList<Pipeline>> ListPipelinesAsync(
        PipelineListQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(query.Top, nameof(query));

        var parameters = new List<string>
        {
            $"$top={query.Top.ToString(CultureInfo.InvariantCulture)}",
            $"api-version={AzureDevOpsConstants.ApiVersion}",
        };

        if (!string.IsNullOrEmpty(query.Name))
        {
            parameters.Add($"name={Uri.EscapeDataString(query.Name)}");
        }

        if (!string.IsNullOrEmpty(query.Folder))
        {
            parameters.Add($"path={Uri.EscapeDataString(query.Folder)}");
        }

        var url = $"{_serverUrl}/{_collection}/{_project}/_apis/build/definitions?{string.Join('&', parameters)}";

        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var dto = await response.Content.ReadFromJsonAsync(
            AzureDevOpsJsonContext.Default.BuildDefinitionListResponse, cancellationToken)
            ?? throw new InvalidOperationException("Failed to deserialize build definition list response.");

        return dto.Value
            .Select(definition => new Pipeline
            {
                Id = definition.Id,
                Name = definition.Name,
                Path = definition.Path,
            })
            .ToList();
    }

    private static TimelineRecord ToTimelineRecord(TimelineRecordResponse dto)
    {
        return new TimelineRecord
        {
            Id = dto.Id,
            ParentId = dto.ParentId,
            Type = dto.Type,
            Name = dto.Name,
            Identifier = dto.Identifier,
            State = dto.State,
            Result = dto.Result,
            LogId = dto.Log?.Id,
            ErrorCount = dto.ErrorCount ?? 0,
            WarningCount = dto.WarningCount ?? 0,
            Order = dto.Order,
            Issues = (dto.Issues ?? []).Select(ToTimelineIssue).ToList(),
        };
    }

    private static TimelineIssue ToTimelineIssue(TimelineIssueResponse dto)
    {
        int? line = null;
        if (dto.Data is not null
            && dto.Data.TryGetValue("logFileLineNumber", out var text)
            && int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            line = parsed;
        }

        return new TimelineIssue { Type = dto.Type, Message = dto.Message, Line = line };
    }

    private static PipelineRun ToPipelineRun(BuildResponse dto)
    {
        return new PipelineRun
        {
            Id = dto.Id,
            PipelineId = dto.Definition?.Id ?? 0,
            PipelineName = dto.Definition?.Name ?? string.Empty,
            BuildNumber = dto.BuildNumber,
            Status = dto.Status,
            Result = dto.Result,
            Reason = dto.Reason,
            SourceBranch = dto.SourceBranch,
            SourceVersion = dto.SourceVersion,
            RequestedFor = dto.RequestedFor?.DisplayName ?? string.Empty,
            QueueTime = dto.QueueTime,
            StartTime = dto.StartTime,
            FinishTime = dto.FinishTime,
            WebUrl = dto.Links?.Web?.Href ?? string.Empty,
        };
    }
}
