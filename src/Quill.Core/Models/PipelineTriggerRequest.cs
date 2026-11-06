namespace Quill.Core.Models;

public class PipelineTriggerRequest
{
    public required int PipelineId { get; init; }

    public string? Branch { get; init; }

    public IReadOnlyDictionary<string, string> Parameters { get; init; } = new Dictionary<string, string>(StringComparer.Ordinal);

    public IReadOnlyList<string> StagesToSkip { get; init; } = [];
}
