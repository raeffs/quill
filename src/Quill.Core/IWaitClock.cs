namespace Quill.Core;

public interface IWaitClock
{
    DateTimeOffset GetUtcNow();

    Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken);
}
