namespace Quill.Core;

/// <summary>
/// The server refused a work item write, for example a missing link target or a
/// cycle. Derives from HttpRequestException so the commands' catch filters hold.
/// </summary>
public sealed class WorkItemRejectedException : HttpRequestException
{
    public WorkItemRejectedException()
    {
    }

    public WorkItemRejectedException(string message)
        : base(message)
    {
    }

    public WorkItemRejectedException(string message, Exception inner)
        : base(message, inner)
    {
    }
}
