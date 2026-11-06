using System.Net;

namespace Quill.Core;

/// <summary>
/// A failed response that carried a Retry-After header. Derives from HttpRequestException
/// so the commands' catch filters hold.
/// </summary>
public sealed class RetryAfterHttpRequestException : HttpRequestException
{
    public RetryAfterHttpRequestException()
    {
    }

    public RetryAfterHttpRequestException(string message)
        : base(message)
    {
    }

    public RetryAfterHttpRequestException(string message, Exception inner)
        : base(message, inner)
    {
    }

    public RetryAfterHttpRequestException(string message, HttpStatusCode statusCode, TimeSpan retryAfter)
        : base(message, null, statusCode)
    {
        RetryAfter = retryAfter;
    }

    public TimeSpan RetryAfter { get; }
}
