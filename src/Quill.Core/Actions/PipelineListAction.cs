using Quill.Core.Models;

namespace Quill.Core.Actions;

public static class PipelineListAction
{
    public static async Task<IReadOnlyList<PipelineResult>> RunAsync(
        IAzureDevOpsBuildClient client,
        PipelineListQuery query,
        CancellationToken cancellationToken)
    {
        var pipelines = await client.ListPipelinesAsync(query, cancellationToken);
        return pipelines.Select(PipelineResultBuilder.Build).ToList();
    }
}
