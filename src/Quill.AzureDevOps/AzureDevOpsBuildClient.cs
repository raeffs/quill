using System.Globalization;
using Quill.AzureDevOps.Dto;
using Quill.Core;
using Quill.Core.Models;

namespace Quill.AzureDevOps;

public class AzureDevOpsBuildClient(AzureDevOpsServer server) : IAzureDevOpsBuildClient
{
    private const string InvalidStateChangeTypeKey = "BuildStatusInvalidChangeException";

    public async Task<PipelineRun> GetRunAsync(int runId, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(runId);

        var request = ServerRequest.Get(ServerScope.Project, $"_apis/build/builds/{runId.ToString(CultureInfo.InvariantCulture)}");
        var dto = await server.ReadAsync(request, AzureDevOpsJsonContext.Default.BuildResponse, cancellationToken);

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

        var runRequest = ServerRequest.Post(
            ServerScope.Project,
            $"_apis/pipelines/{request.PipelineId.ToString(CultureInfo.InvariantCulture)}/runs",
            body,
            AzureDevOpsJsonContext.Default.RunPipelineBody) with
        { RejectedWhen = IsInvalidStateChange };
        var queued = await server.ReadAsync(runRequest, AzureDevOpsJsonContext.Default.QueuedRunResponse, cancellationToken);

        return await LaterStep.RunAsync(
            $"Queued run {queued.Id}",
            "reading it",
            () => GetRunAsync(queued.Id, CancellationToken.None));
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
            "?retry=true",
            new UpdateBuildBody(),
            cancellationToken);
    }

    public async Task<IReadOnlyList<TimelineRecord>> GetTimelineAsync(int runId, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(runId);

        var request = ServerRequest.Get(ServerScope.Project, $"_apis/build/builds/{runId.ToString(CultureInfo.InvariantCulture)}/timeline");
        var dto = await server.ReadOrNothingAsync(request, AzureDevOpsJsonContext.Default.TimelineResponse, cancellationToken);
        if (dto is null)
        {
            return [];
        }

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

        var path = $"_apis/build/builds/{runId.ToString(CultureInfo.InvariantCulture)}/logs/{logId.ToString(CultureInfo.InvariantCulture)}";
        var separator = '?';

        if (startLine is { } start)
        {
            path += $"{separator}startLine={start.ToString(CultureInfo.InvariantCulture)}";
            separator = '&';
        }

        if (endLine is { } end)
        {
            path += $"{separator}endLine={end.ToString(CultureInfo.InvariantCulture)}";
        }

        var request = ServerRequest.Get(ServerScope.Project, path);
        var dto = await server.ReadOrNothingAsync(request, AzureDevOpsJsonContext.Default.BuildLogLinesResponse, cancellationToken);
        if (dto is null)
        {
            return [];
        }

        return dto.Value.ToList();
    }

    public async Task<int> GetLogLineCountAsync(int runId, int logId, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(runId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(logId);

        var request = ServerRequest.Get(ServerScope.Project, $"_apis/build/builds/{runId.ToString(CultureInfo.InvariantCulture)}/logs");
        var dto = await server.ReadAsync(request, AzureDevOpsJsonContext.Default.BuildLogListResponse, cancellationToken);

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

        var request = ServerRequest.Get(ServerScope.Project, $"_apis/build/builds?{string.Join('&', parameters)}");
        var dto = await server.ReadAsync(request, AzureDevOpsJsonContext.Default.BuildListResponse, cancellationToken);

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
        };

        if (!string.IsNullOrEmpty(query.Name))
        {
            parameters.Add($"name={Uri.EscapeDataString(query.Name)}");
        }

        if (!string.IsNullOrEmpty(query.Folder))
        {
            parameters.Add($"path={Uri.EscapeDataString(query.Folder)}");
        }

        var request = ServerRequest.Get(ServerScope.Project, $"_apis/build/definitions?{string.Join('&', parameters)}");
        var dto = await server.ReadAsync(request, AzureDevOpsJsonContext.Default.BuildDefinitionListResponse, cancellationToken);

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

        var request = ServerRequest.Get(ServerScope.Project, $"_apis/test/runs?buildUri=vstfs:///Build/Build/{runId.ToString(CultureInfo.InvariantCulture)}");
        var dto = await server.ReadAsync(request, AzureDevOpsJsonContext.Default.TestRunListResponse, cancellationToken);

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

        var request = ServerRequest.Get(
            ServerScope.Project,
            $"_apis/test/Runs/{testRunId.ToString(CultureInfo.InvariantCulture)}/results?{string.Join('&', parameters)}");
        var dto = await server.ReadAsync(request, AzureDevOpsJsonContext.Default.TestResultListResponse, cancellationToken);

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
        var request = ServerRequest.Patch(
            ServerScope.Project,
            $"_apis/build/builds/{runId.ToString(CultureInfo.InvariantCulture)}{query}",
            body,
            AzureDevOpsJsonContext.Default.UpdateBuildBody) with
        { RejectedWhen = IsInvalidStateChange };
        var dto = await server.ReadAsync(request, AzureDevOpsJsonContext.Default.BuildResponse, cancellationToken);

        return ToPipelineRun(dto);
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
