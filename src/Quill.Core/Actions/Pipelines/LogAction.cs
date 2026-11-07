using Quill.Core.Models;

namespace Quill.Core.Actions.Pipelines;

public sealed record PipelineLogRequest(int RunId, int LogId, int? StartLine, int? EndLine, int? Tail);

public static class LogAction
{
    public static async Task<PipelineLogResult> RunAsync(
        IAzureDevOpsBuildClient client,
        PipelineLogRequest request,
        CancellationToken cancellationToken)
    {
        var startLine = request.StartLine;
        var endLine = request.EndLine;
        if (request.Tail is not null)
        {
            var lineCount = await client.GetLogLineCountAsync(request.RunId, request.LogId, cancellationToken);
            startLine = PipelineLogResultBuilder.TailStartLine(lineCount, request.Tail.Value);
            endLine = null;
        }

        var lines = await client.GetLogLinesAsync(request.RunId, request.LogId, startLine, endLine, cancellationToken);
        return PipelineLogResultBuilder.Build(request.RunId, request.LogId, startLine, lines);
    }
}
