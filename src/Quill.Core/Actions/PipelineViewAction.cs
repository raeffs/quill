using Quill.Core.Models;

namespace Quill.Core.Actions;

public static class PipelineViewAction
{
    public static async Task<PipelineRunResult> RunAsync(
        IAzureDevOpsBuildClient client,
        int runId,
        bool withStages,
        CancellationToken cancellationToken)
    {
        var run = await client.GetRunAsync(runId, cancellationToken);
        IReadOnlyList<PipelineStageResult>? stages = null;
        if (withStages)
        {
            var records = await client.GetTimelineAsync(runId, cancellationToken);
            stages = PipelineStageTreeBuilder.Build(records);
        }

        return PipelineRunResultBuilder.Build(run, stages);
    }
}
