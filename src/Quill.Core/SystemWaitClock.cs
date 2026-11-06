namespace Quill.Core;

public sealed class SystemWaitClock : IWaitClock
{
    public static SystemWaitClock Instance { get; } = new();

    public DateTimeOffset GetUtcNow() => TimeProvider.System.GetUtcNow();

    public Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken) => Task.Delay(delay, cancellationToken);
}
