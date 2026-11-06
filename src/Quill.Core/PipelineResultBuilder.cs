using Quill.Core.Models;

namespace Quill.Core;

public static class PipelineResultBuilder
{
    public static PipelineResult Build(Pipeline pipeline)
    {
        return new PipelineResult
        {
            Id = pipeline.Id,
            Name = pipeline.Name,
            Folder = pipeline.Path,
        };
    }
}
