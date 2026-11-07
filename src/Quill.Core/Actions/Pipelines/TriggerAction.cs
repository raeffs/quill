using Quill.Core.Models;

namespace Quill.Core.Actions.Pipelines;

public static class TriggerAction
{
    public static async Task<PipelineRunResult> RunAsync(
        IAzureDevOpsBuildClient client,
        PipelineTriggerRequest request,
        CancellationToken cancellationToken)
    {
        var run = await client.QueueRunAsync(request, cancellationToken);
        return PipelineRunResultBuilder.Build(run, null);
    }
}
