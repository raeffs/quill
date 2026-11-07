using System.Globalization;
using Quill.Core;
using Quill.Core.Models;

namespace Quill.Tests.Shared.Fakes;

public sealed class ScriptedBuildClient : IAzureDevOpsBuildClient
{
    public ScriptedBuildClient()
    {
    }

    public ScriptedBuildClient(params Func<PipelineRun>[] polls)
    {
        var next = 0;
        GetRun = _ => polls[Math.Min(next++, polls.Length - 1)]();
    }

#pragma warning disable CA1002, MA0016 // Tests assert on the recorded calls as a list
    public List<string> Calls { get; } = [];
#pragma warning restore CA1002, MA0016

    public Func<int, PipelineRun> GetRun { get; set; } = Unscripted<int, PipelineRun>();

    public Func<PipelineTriggerRequest, PipelineRun> QueueRun { get; set; } = Unscripted<PipelineTriggerRequest, PipelineRun>();

    public Func<int, PipelineRun> CancelRun { get; set; } = Unscripted<int, PipelineRun>();

    public Func<int, PipelineRun> RetryRun { get; set; } = Unscripted<int, PipelineRun>();

    public Func<PipelineListQuery, IReadOnlyList<Pipeline>> ListPipelines { get; set; } = Unscripted<PipelineListQuery, IReadOnlyList<Pipeline>>();

    public Func<PipelineRunListQuery, IReadOnlyList<PipelineRun>> ListRuns { get; set; } = Unscripted<PipelineRunListQuery, IReadOnlyList<PipelineRun>>();

    public Func<int, IReadOnlyList<TimelineRecord>> GetTimeline { get; set; } = Unscripted<int, IReadOnlyList<TimelineRecord>>();

    public Func<int, int, int?, int?, IReadOnlyList<string>> GetLogLines { get; set; } = Unscripted<int, int, int?, int?, IReadOnlyList<string>>();

    public Func<int, int, int> GetLogLineCount { get; set; } = Unscripted<int, int, int>();

    public Func<int, IReadOnlyList<TestRun>> ListTestRuns { get; set; } = Unscripted<int, IReadOnlyList<TestRun>>();

    public Func<int, TestResultQuery, IReadOnlyList<TestCaseResult>> ListTestResults { get; set; } = Unscripted<int, TestResultQuery, IReadOnlyList<TestCaseResult>>();

    public Task<PipelineRun> GetRunAsync(int runId, CancellationToken cancellationToken = default)
        => Record($"GetRun({Format(runId)})", () => GetRun(runId));

    public Task<PipelineRun> QueueRunAsync(PipelineTriggerRequest request, CancellationToken cancellationToken = default)
        => Record($"QueueRun({Format(request.PipelineId)})", () => QueueRun(request));

    public Task<PipelineRun> CancelRunAsync(int runId, CancellationToken cancellationToken = default)
        => Record($"CancelRun({Format(runId)})", () => CancelRun(runId));

    public Task<PipelineRun> RetryRunAsync(int runId, CancellationToken cancellationToken = default)
        => Record($"RetryRun({Format(runId)})", () => RetryRun(runId));

    public Task<IReadOnlyList<Pipeline>> ListPipelinesAsync(PipelineListQuery query, CancellationToken cancellationToken = default)
        => Record("ListPipelines", () => ListPipelines(query));

    public Task<IReadOnlyList<PipelineRun>> ListRunsAsync(PipelineRunListQuery query, CancellationToken cancellationToken = default)
        => Record("ListRuns", () => ListRuns(query));

    public Task<IReadOnlyList<TimelineRecord>> GetTimelineAsync(int runId, CancellationToken cancellationToken = default)
        => Record($"GetTimeline({Format(runId)})", () => GetTimeline(runId));

    public Task<IReadOnlyList<string>> GetLogLinesAsync(int runId, int logId, int? startLine, int? endLine, CancellationToken cancellationToken = default)
        => Record($"GetLogLines({Format(runId)},{Format(logId)},{Format(startLine)},{Format(endLine)})", () => GetLogLines(runId, logId, startLine, endLine));

    public Task<int> GetLogLineCountAsync(int runId, int logId, CancellationToken cancellationToken = default)
        => Record($"GetLogLineCount({Format(runId)},{Format(logId)})", () => GetLogLineCount(runId, logId));

    public Task<IReadOnlyList<TestRun>> ListTestRunsAsync(int runId, CancellationToken cancellationToken = default)
        => Record($"ListTestRuns({Format(runId)})", () => ListTestRuns(runId));

    public Task<IReadOnlyList<TestCaseResult>> ListTestResultsAsync(int testRunId, TestResultQuery query, CancellationToken cancellationToken = default)
        => Record($"ListTestResults({Format(testRunId)})", () => ListTestResults(testRunId, query));

    private static string Format(int? value) => value?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;

    private static Func<T, TResult> Unscripted<T, TResult>() => _ => throw new NotSupportedException();

    private static Func<T1, T2, TResult> Unscripted<T1, T2, TResult>() => (_, _) => throw new NotSupportedException();

    private static Func<T1, T2, T3, T4, TResult> Unscripted<T1, T2, T3, T4, TResult>() => (_, _, _, _) => throw new NotSupportedException();

    private Task<T> Record<T>(string call, Func<T> script)
    {
        Calls.Add(call);
        return Task.FromResult(script());
    }
}
