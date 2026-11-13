namespace Quill.Core;

public sealed class UnavailableException : QuillException
{
    public UnavailableException()
    {
    }

    public UnavailableException(string message)
        : base(message)
    {
    }

    public UnavailableException(string message, Exception inner)
        : base(message, inner)
    {
    }

    public UnavailableException(string message, TimeSpan? retryAfter, Exception? inner = null)
        : base(message, inner)
    {
        RetryAfter = retryAfter;
    }

    public TimeSpan? RetryAfter { get; }

    public override int ExitCode => 2;
}
