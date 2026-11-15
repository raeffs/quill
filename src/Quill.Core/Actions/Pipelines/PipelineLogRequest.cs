namespace Quill.Core.Actions.Pipelines;

public sealed record PipelineLogRequest(int RunId, int LogId, int? StartLine, int? EndLine, int? Tail);
