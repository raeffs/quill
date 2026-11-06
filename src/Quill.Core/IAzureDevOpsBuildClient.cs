using Quill.Core.Models;

namespace Quill.Core;

public interface IAzureDevOpsBuildClient
{
    Task<PipelineRun> GetRunAsync(int runId, CancellationToken cancellationToken = default);
}
