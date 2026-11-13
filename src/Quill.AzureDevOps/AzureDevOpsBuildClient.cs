using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Quill.AzureDevOps.Dto;
using Quill.Core;
using Quill.Core.Models;

namespace Quill.AzureDevOps;

public class AzureDevOpsBuildClient : IAzureDevOpsBuildClient
{
    private const string InvalidStateChangeTypeKey = "BuildStatusInvalidChangeException";

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

        var dto = await response.Content.ReadFromJsonAsync(
            AzureDevOpsJsonContext.Default.BuildResponse, cancellationToken)
            ?? throw new DefectException("Failed to deserialize build response.");

        return ToPipelineRun(dto);
    }

    public async Task<PipelineRun> QueueRunAsync(
        PipelineTriggerRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(request.PipelineId, nameof(request));

        var body = new RunPipelineBody
        {
            Resources = request.Branch is null
                ? null
                : new RunPipelineResources
                {
                    Repositories = new RunPipelineRepositories
                    {
                        Self = new RunPipelineRepository { RefName = RefNames.FromBranch(request.Branch) },
                    },
                },
            TemplateParameters = request.Parameters.Count == 0 ? null : request.Parameters,
            StagesToSkip = request.StagesToSkip.Count == 0 ? null : request.StagesToSkip,
        };

        var url = $"{_serverUrl}/{_collection}/{_project}/_apis/pipelines/{request.PipelineId.ToString(CultureInfo.InvariantCulture)}/runs?api-version={AzureDevOpsConstants.ApiVersion}";

        var json = JsonSerializer.Serialize(body, AzureDevOpsJsonContext.Default.RunPipelineBody);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        using var runRequest = RunChangeRequest(HttpMethod.Post, url, content);
        using var response = await _httpClient.SendAsync(runRequest, cancellationToken);

        var queued = await response.Content.ReadFromJsonAsync(
            AzureDevOpsJsonContext.Default.QueuedRunResponse, cancellationToken)
            ?? throw new DefectException("Failed to deserialize queued run response.");

        try
        {
            return await GetRunAsync(queued.Id, cancellationToken);
        }
        catch (UnavailableException exception)
        {
            throw new UnavailableException(ReadFailed(queued.Id, exception), exception.RetryAfter, exception);
        }
        catch (RejectedException exception)
        {
            throw new RejectedException(ReadFailed(queued.Id, exception), exception);
        }
        catch (DeniedException exception)
        {
            throw new DeniedException(ReadFailed(queued.Id, exception), exception);
        }
    }

    public Task<PipelineRun> CancelRunAsync(int runId, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(runId);

        return UpdateRunAsync(
            runId,
            string.Empty,
            new UpdateBuildBody { Status = "cancelling" },
            cancellationToken);
    }

    public Task<PipelineRun> RetryRunAsync(int runId, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(runId);

        return UpdateRunAsync(
            runId,
            "&retry=true",
            new UpdateBuildBody(),
            cancellationToken);
    }

    public async Task<IReadOnlyList<TimelineRecord>> GetTimelineAsync(int runId, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(runId);

        var url = $"{_serverUrl}/{_collection}/{_project}/_apis/build/builds/{runId.ToString(CultureInfo.InvariantCulture)}/timeline?api-version={AzureDevOpsConstants.ApiVersion}";

        var response = await _httpClient.GetAsync(url, cancellationToken);

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
            ?? throw new DefectException("Failed to deserialize timeline response.");

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
            ?? throw new DefectException("Failed to deserialize build log response.");

        return dto.Value.ToList();
    }

    public async Task<int> GetLogLineCountAsync(int runId, int logId, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(runId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(logId);

        var url = $"{_serverUrl}/{_collection}/{_project}/_apis/build/builds/{runId.ToString(CultureInfo.InvariantCulture)}/logs?api-version={AzureDevOpsConstants.ApiVersion}";

        var response = await _httpClient.GetAsync(url, cancellationToken);

        var dto = await response.Content.ReadFromJsonAsync(
            AzureDevOpsJsonContext.Default.BuildLogListResponse, cancellationToken)
            ?? throw new DefectException("Failed to deserialize build log list response.");

        var log = dto.Value.FirstOrDefault(entry => entry.Id == logId)
            ?? throw new RejectedException($"Log {logId} not found in run {runId}.");

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

        var dto = await response.Content.ReadFromJsonAsync(
            AzureDevOpsJsonContext.Default.BuildListResponse, cancellationToken)
            ?? throw new DefectException("Failed to deserialize build list response.");

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

        var dto = await response.Content.ReadFromJsonAsync(
            AzureDevOpsJsonContext.Default.BuildDefinitionListResponse, cancellationToken)
            ?? throw new DefectException("Failed to deserialize build definition list response.");

        return dto.Value
            .Select(definition => new Pipeline
            {
                Id = definition.Id,
                Name = definition.Name,
                Path = definition.Path,
            })
            .ToList();
    }

    public async Task<IReadOnlyList<TestRun>> ListTestRunsAsync(int runId, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(runId);

        var url = $"{_serverUrl}/{_collection}/{_project}/_apis/test/runs?buildUri=vstfs:///Build/Build/{runId.ToString(CultureInfo.InvariantCulture)}&api-version={AzureDevOpsConstants.ApiVersion}";

        var response = await _httpClient.GetAsync(url, cancellationToken);

        var dto = await response.Content.ReadFromJsonAsync(
            AzureDevOpsJsonContext.Default.TestRunListResponse, cancellationToken)
            ?? throw new DefectException("Failed to deserialize test run list response.");

        return dto.Value
            .Select(run => new TestRun
            {
                Id = run.Id,
                Name = run.Name,
                State = run.State,
                TotalTests = run.TotalTests,
                PassedTests = run.PassedTests,
                UnanalyzedTests = run.UnanalyzedTests,
                NotApplicableTests = run.NotApplicableTests,
            })
            .ToList();
    }

    public async Task<IReadOnlyList<TestCaseResult>> ListTestResultsAsync(
        int testRunId,
        TestResultQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(testRunId);
        ArgumentNullException.ThrowIfNull(query);
        ArgumentOutOfRangeException.ThrowIfZero(query.Outcomes.Count, nameof(query));

        var parameters = new List<string>
        {
            $"outcomes={string.Join(',', query.Outcomes.Select(Uri.EscapeDataString))}",
        };

        if (query.Top is { } top)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(top, 1, nameof(query));
            parameters.Add($"$top={top.ToString(CultureInfo.InvariantCulture)}");
        }

        if (query.Skip is { } skip)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(skip, nameof(query));
            parameters.Add($"$skip={skip.ToString(CultureInfo.InvariantCulture)}");
        }

        parameters.Add($"api-version={AzureDevOpsConstants.ApiVersion}");

        var url = $"{_serverUrl}/{_collection}/{_project}/_apis/test/Runs/{testRunId.ToString(CultureInfo.InvariantCulture)}/results?{string.Join('&', parameters)}";

        var response = await _httpClient.GetAsync(url, cancellationToken);

        var dto = await response.Content.ReadFromJsonAsync(
            AzureDevOpsJsonContext.Default.TestResultListResponse, cancellationToken)
            ?? throw new DefectException("Failed to deserialize test result list response.");

        return dto.Value
            .Select(result => new TestCaseResult
            {
                Title = result.TestCaseTitle,
                Name = result.AutomatedTestName,
                Storage = result.AutomatedTestStorage,
                Outcome = result.Outcome,
                Message = result.ErrorMessage,
                StackTrace = result.StackTrace,
                DurationInMs = result.DurationInMs,
                FailingSince = result.FailingSince?.Date,
                FailingSinceRunId = result.FailingSince?.Build?.Id,
            })
            .ToList();
    }

    private async Task<PipelineRun> UpdateRunAsync(
        int runId,
        string query,
        UpdateBuildBody body,
        CancellationToken cancellationToken)
    {
        var url = $"{_serverUrl}/{_collection}/{_project}/_apis/build/builds/{runId.ToString(CultureInfo.InvariantCulture)}?api-version={AzureDevOpsConstants.ApiVersion}{query}";

        var json = JsonSerializer.Serialize(body, AzureDevOpsJsonContext.Default.UpdateBuildBody);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        using var request = RunChangeRequest(HttpMethod.Patch, url, content);
        using var response = await _httpClient.SendAsync(request, cancellationToken);

        var dto = await response.Content.ReadFromJsonAsync(
            AzureDevOpsJsonContext.Default.BuildResponse, cancellationToken)
            ?? throw new DefectException("Failed to deserialize build response.");

        return ToPipelineRun(dto);
    }

    private static string ReadFailed(int runId, QuillException exception) =>
        $"Queued run {runId}, but reading it failed: {exception.Message}";

    private static HttpRequestMessage RunChangeRequest(HttpMethod method, string url, HttpContent content)
    {
        var request = new HttpRequestMessage(method, url) { Content = content };
        request.Options.Set(FailureModeHandler.RejectedWhen, IsInvalidStateChange);
        return request;
    }

    // The server reports an invalid build state change as a 500.
    private static bool IsInvalidStateChange(ServerError error) =>
        error.Status == 500
        && string.Equals(error.TypeKey, InvalidStateChangeTypeKey, StringComparison.Ordinal);

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
