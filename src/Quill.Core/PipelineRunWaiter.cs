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
        UnavailableException? lastError = null;

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
            catch (UnavailableException ex)
            {
                lastError = ex;
                if (ex.RetryAfter is { } retryAfter && retryAfter > wait)
                {
                    wait = retryAfter;
                }
            }

            var left = deadline - clock.GetUtcNow();
            if (left <= TimeSpan.Zero)
            {
                return last ?? throw new UnavailableException(
                    string.Create(CultureInfo.InvariantCulture, $"No poll of run {runId} succeeded before the timeout: {lastError?.Message}"),
                    lastError?.RetryAfter,
                    lastError);
            }

            await clock.DelayAsync(wait < left ? wait : left, cancellationToken);
        }
    }
}
