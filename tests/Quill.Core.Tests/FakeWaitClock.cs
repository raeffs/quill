namespace Quill.Core.Tests;

internal sealed class FakeWaitClock : IWaitClock
{
    private DateTimeOffset _now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public List<TimeSpan> Delays { get; } = [];

    public DateTimeOffset GetUtcNow() => _now;

    public Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Delays.Add(delay);
        _now += delay;
        return Task.CompletedTask;
    }
}
