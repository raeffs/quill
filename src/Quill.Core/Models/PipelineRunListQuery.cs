namespace Quill.Core.Models;

public class PipelineRunListQuery
{
    public int? PipelineId { get; init; }

    public string? Branch { get; init; }

    public string? Status { get; init; }

    public string? Result { get; init; }

    public string? Reason { get; init; }

    public required int Top { get; init; }
}
