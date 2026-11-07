using Quill.Core.Models;

namespace Quill.Core.Actions;

public sealed record PipelineTestsRequest(int RunId, bool WithResults, TestResultQuery Query);

public static class PipelineTestsAction
{
    public static async Task<IReadOnlyList<PipelineTestRunResult>> RunAsync(
        IAzureDevOpsBuildClient client,
        PipelineTestsRequest request,
        CancellationToken cancellationToken)
    {
        var testRuns = await client.ListTestRunsAsync(request.RunId, cancellationToken);
        var rows = new List<PipelineTestRunResult>();
        foreach (var testRun in testRuns)
        {
            IReadOnlyList<TestCaseResult>? results = null;
            if (request.WithResults)
            {
                results = await client.ListTestResultsAsync(testRun.Id, request.Query, cancellationToken);
            }

            rows.Add(PipelineTestRunResultBuilder.Build(testRun, results));
        }

        return rows;
    }
}
