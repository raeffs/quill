namespace Quill.Core;

public abstract class QuillException : Exception
{
    protected QuillException()
    {
    }

    protected QuillException(string message)
        : base(message)
    {
    }

    protected QuillException(string message, Exception? inner)
        : base(message, inner)
    {
    }

    public abstract int ExitCode { get; }
}
