namespace Quill.AzureDevOps;

internal static class RetryAfterHeader
{
    public static TimeSpan? Read(HttpResponseMessage response)
    {
        ArgumentNullException.ThrowIfNull(response);

        var retryAfter = response.Headers.RetryAfter;
        if (retryAfter is null)
        {
            return null;
        }

        if (retryAfter.Delta is TimeSpan delta)
        {
            return delta;
        }

        if (retryAfter.Date is DateTimeOffset date)
        {
            var diff = date - DateTimeOffset.UtcNow;
            return diff > TimeSpan.Zero ? diff : TimeSpan.Zero;
        }

        return null;
    }
}
