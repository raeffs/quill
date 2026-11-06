namespace Quill.Core;

/// <summary>
/// The server refused a pipeline run, for example a missing required parameter or
/// an unknown stage. Derives from HttpRequestException so the commands' catch
/// filters hold.
/// </summary>
public sealed class PipelineRejectedException : HttpRequestException
{
    public PipelineRejectedException()
    {
    }

    public PipelineRejectedException(string message)
        : base(message)
    {
    }

    public PipelineRejectedException(string message, Exception inner)
        : base(message, inner)
    {
    }
}
