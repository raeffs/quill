using Quill.Core.Models;

namespace Quill.Core.Actions.Pipelines;

public static class RunsAction
{
    public static async Task<IReadOnlyList<PipelineRunResult>> RunAsync(
        IAzureDevOpsBuildClient client,
        PipelineRunListQuery query,
        CancellationToken cancellationToken)
    {
        var runs = await client.ListRunsAsync(query, cancellationToken);
        return runs.Select(run => PipelineRunResultBuilder.Build(run)).ToList();
    }
}
