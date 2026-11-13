namespace Quill.Core;

public sealed class RejectedException : QuillException
{
    public RejectedException()
    {
    }

    public RejectedException(string message)
        : base(message)
    {
    }

    public RejectedException(string message, Exception inner)
        : base(message, inner)
    {
    }

    public override int ExitCode => 1;
}
