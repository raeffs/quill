using Quill.Core.Models;

namespace Quill.Core.Tests;

internal sealed class FakeBuildClient(params Func<PipelineRun>[] polls) : IAzureDevOpsBuildClient
{
    public int GetRunCalls { get; private set; }

    public Task<PipelineRun> GetRunAsync(int runId, CancellationToken cancellationToken = default)
    {
        var step = polls[Math.Min(GetRunCalls, polls.Length - 1)];
        GetRunCalls++;
        return Task.FromResult(step());
    }

    public Task<PipelineRun> QueueRunAsync(PipelineTriggerRequest request, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<IReadOnlyList<Pipeline>> ListPipelinesAsync(PipelineListQuery query, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<IReadOnlyList<PipelineRun>> ListRunsAsync(PipelineRunListQuery query, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<IReadOnlyList<TimelineRecord>> GetTimelineAsync(int runId, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<IReadOnlyList<string>> GetLogLinesAsync(int runId, int logId, int? startLine, int? endLine, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<int> GetLogLineCountAsync(int runId, int logId, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();
}
