using Quill.Core.Models;

namespace Quill.Core.Actions.Pipelines;

public static class WaitAction
{
    public static async Task<PipelineRunResult> RunAsync(
        IAzureDevOpsBuildClient client,
        IWaitClock clock,
        int runId,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var run = await new PipelineRunWaiter(client, clock).WaitAsync(runId, timeout, cancellationToken);
        return PipelineRunResultBuilder.Build(run, null);
    }
}
