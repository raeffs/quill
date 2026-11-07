using Quill.Core.Models;

namespace Quill.Core.Actions.Pipelines;

public static class RetryAction
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
