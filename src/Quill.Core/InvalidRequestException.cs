namespace Quill.Core;

/// <summary>
/// Quill refused the request before or after reading the server: a frontmatter check,
/// a parent type mismatch. Not a server verdict.
/// </summary>
public sealed class InvalidRequestException : Exception
{
    public InvalidRequestException()
    {
    }

    public InvalidRequestException(string message)
        : base(message)
    {
    }

    public InvalidRequestException(string message, Exception inner)
        : base(message, inner)
    {
    }
}
