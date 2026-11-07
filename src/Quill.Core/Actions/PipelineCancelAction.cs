using Quill.Core.Models;

namespace Quill.Core.Actions;

public static class PipelineCancelAction
{
    public static async Task<PipelineRunResult> RunAsync(
        IAzureDevOpsBuildClient client,
        int runId,
        CancellationToken cancellationToken)
    {
        var run = await client.CancelRunAsync(runId, cancellationToken);
        return PipelineRunResultBuilder.Build(run, null);
    }
}
