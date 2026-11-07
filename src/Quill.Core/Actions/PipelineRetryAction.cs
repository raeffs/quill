using Quill.Core.Models;

namespace Quill.Core.Actions;

public static class PipelineRetryAction
{
    public static async Task<PipelineRunResult> RunAsync(
        IAzureDevOpsBuildClient client,
        int runId,
        CancellationToken cancellationToken)
    {
        var run = await client.RetryRunAsync(runId, cancellationToken);
        return PipelineRunResultBuilder.Build(run, null);
    }
}
