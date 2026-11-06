using Quill.Core.Models;

namespace Quill.Core;

public static class PipelineLogResultBuilder
{
    public static int TailStartLine(int lineCount, int tail)
    {
        return Math.Max(1, lineCount - tail);
    }

    public static PipelineLogResult Build(int runId, int logId, int? sentStartLine, IReadOnlyList<string> lines)
    {
        var startLine = sentStartLine ?? 1;

        return new PipelineLogResult
        {
            RunId = runId,
            LogId = logId,
            StartLine = lines.Count == 0 ? null : startLine,
            EndLine = lines.Count == 0 ? null : startLine + lines.Count - 1,
            Lines = lines,
        };
    }
}
