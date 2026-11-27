namespace Quill.Core;

public sealed class UsageException : QuillException
{
    public UsageException()
    {
    }

    public UsageException(string message)
        : base(message)
    {
    }

    public UsageException(string message, Exception inner)
        : base(message, inner)
    {
    }

    public override int ExitCode => 3;

    public override QuillException WithMessage(string message) => new UsageException(message, this);
}
