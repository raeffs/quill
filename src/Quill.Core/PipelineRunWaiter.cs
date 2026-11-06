using System.Globalization;
using Quill.Core.Models;

namespace Quill.Core;

public sealed class PipelineRunWaiter(IAzureDevOpsBuildClient client, IWaitClock clock)
{
    public static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(10);

    public async Task<PipelineRun> WaitAsync(int runId, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(runId, 0);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(timeout, TimeSpan.Zero);

        var deadline = clock.GetUtcNow() + timeout;
        PipelineRun? last = null;
        HttpRequestException? lastError = null;

        while (true)
        {
            var wait = PollInterval;
            try
            {
                last = await client.GetRunAsync(runId, cancellationToken);
                if (string.Equals(last.Status, "completed", StringComparison.Ordinal))
                {
                    return last;
                }
            }
            catch (HttpRequestException ex) when (IsTransient(ex))
            {
                lastError = ex;
                if (ex is RetryAfterHttpRequestException retryAfter && retryAfter.RetryAfter > wait)
                {
                    wait = retryAfter.RetryAfter;
                }
            }
            catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                lastError = new HttpRequestException(ex.Message, ex);
            }

            var left = deadline - clock.GetUtcNow();
            if (left <= TimeSpan.Zero)
            {
                return last ?? throw new HttpRequestException(
                    string.Create(CultureInfo.InvariantCulture, $"No poll of run {runId} succeeded before the timeout: {lastError?.Message}"),
                    lastError,
                    lastError?.StatusCode);
            }

            await clock.DelayAsync(wait < left ? wait : left, cancellationToken);
        }
    }

    private static bool IsTransient(HttpRequestException ex)
    {
        if (ex.StatusCode is not { } status)
        {
            return true;
        }

        var code = (int)status;
        return code is 408 or 429 or >= 500;
    }
}
