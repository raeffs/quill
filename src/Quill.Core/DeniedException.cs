namespace Quill.Core;

public sealed class DeniedException : QuillException
{
    public DeniedException()
    {
    }

    public DeniedException(string message)
        : base(message)
    {
    }

    public DeniedException(string message, Exception inner)
        : base(message, inner)
    {
    }

    public override int ExitCode => 5;
}
