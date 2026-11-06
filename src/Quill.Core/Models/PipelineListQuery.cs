namespace Quill.Core.Models;

public class PipelineListQuery
{
    public string? Name { get; init; }

    public string? Folder { get; init; }

    public required int Top { get; init; }
}
