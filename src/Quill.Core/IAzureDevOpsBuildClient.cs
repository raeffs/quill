using Quill.Core.Models;

namespace Quill.Core;

public interface IAzureDevOpsBuildClient
{
    Task<PipelineRun> GetRunAsync(int runId, CancellationToken cancellationToken = default);

    Task<PipelineRun> QueueRunAsync(PipelineTriggerRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Pipeline>> ListPipelinesAsync(PipelineListQuery query, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PipelineRun>> ListRunsAsync(PipelineRunListQuery query, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TimelineRecord>> GetTimelineAsync(int runId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetLogLinesAsync(int runId, int logId, int? startLine, int? endLine, CancellationToken cancellationToken = default);

    Task<int> GetLogLineCountAsync(int runId, int logId, CancellationToken cancellationToken = default);
}
