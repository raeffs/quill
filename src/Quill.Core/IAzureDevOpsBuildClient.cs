using Quill.Core.Models;

namespace Quill.Core;

public interface IAzureDevOpsBuildClient
{
    Task<PipelineRun> GetRunAsync(int runId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Pipeline>> ListPipelinesAsync(PipelineListQuery query, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PipelineRun>> ListRunsAsync(PipelineRunListQuery query, CancellationToken cancellationToken = default);
}
