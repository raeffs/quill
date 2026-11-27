namespace Quill.Core;

public sealed class DefectException : QuillException
{
    public DefectException()
    {
    }

    public DefectException(string message)
        : base(message)
    {
    }

    public DefectException(string message, Exception inner)
        : base(message, inner)
    {
    }

    public override int ExitCode => 4;

    public override QuillException WithMessage(string message) => new DefectException(message, this);
}
