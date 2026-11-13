using Quill.Core.Models;
using Quill.Tests.Shared.Fakes;
using Shouldly;

namespace Quill.Core.Tests;

public class PipelineRunWaiterTests
{
    private static readonly TimeSpan Ten = TimeSpan.FromSeconds(10);

    public static TheoryData<Exception> TransientFailures => new()
    {
        new UnavailableException("The server answered 500 (Internal Server Error)."),
        new UnavailableException("The server answered 503 (Service Unavailable)."),
        new UnavailableException("The server answered 429 (Too Many Requests)."),
        new UnavailableException("The server answered 408 (Request Timeout)."),
        new UnavailableException("No answer from the server: network down"),
        new UnavailableException("The server did not answer within 30 seconds."),
    };

    public static TheoryData<Exception> OtherFailures => new()
    {
        new DeniedException("The server answered 403 (Forbidden)."),
        new RejectedException("The server answered 404 (Not Found)."),
        new RejectedException("The server answered 400 (Bad Request)."),
        new DefectException("The server answered 302 (Found)."),
    };

    [Theory]
    [InlineData("succeeded")]
    [InlineData("partiallySucceeded")]
    [InlineData("failed")]
    [InlineData("canceled")]
    public async Task WaitAsync_AlreadyCompleted_ReturnsAfterOneRequest(string result)
    {
        // Arrange
        var completed = Run("completed", result);
        var client = new ScriptedBuildClient(() => completed);
        var clock = new FakeWaitClock();

        // Act
        var run = await new PipelineRunWaiter(client, clock).WaitAsync(4202, TimeSpan.FromSeconds(600), TestContext.Current.CancellationToken);

        // Assert
        run.ShouldBeSameAs(completed);
        client.Calls.Count.ShouldBe(1);
        clock.Delays.ShouldBeEmpty();
    }

    [Fact]
    public async Task WaitAsync_InProgressThenCompleted_PollsEveryTenSeconds()
    {
        // Arrange
        var completed = Run("completed", "succeeded");
        var client = new ScriptedBuildClient(() => Run("inProgress"), () => Run("inProgress"), () => completed);
        var clock = new FakeWaitClock();

        // Act
        var run = await new PipelineRunWaiter(client, clock).WaitAsync(4202, TimeSpan.FromSeconds(600), TestContext.Current.CancellationToken);

        // Assert
        run.ShouldBeSameAs(completed);
        client.Calls.Count.ShouldBe(3);
        clock.Delays.ShouldBe([Ten, Ten]);
    }

    [Fact]
    public async Task WaitAsync_Cancelling_KeepsWaiting()
    {
        // Arrange
        var completed = Run("completed", "canceled");
        var client = new ScriptedBuildClient(() => Run("cancelling"), () => completed);

        // Act
        var run = await new PipelineRunWaiter(client, new FakeWaitClock()).WaitAsync(4202, TimeSpan.FromSeconds(600), TestContext.Current.CancellationToken);

        // Assert
        run.ShouldBeSameAs(completed);
        client.Calls.Count.ShouldBe(2);
    }

    [Fact]
    public async Task WaitAsync_Timeout_ReturnsTheRowOfTheLastPoll()
    {
        // Arrange
        var client = new ScriptedBuildClient(
            () => Run("inProgress", buildNumber: "p1"),
            () => Run("inProgress", buildNumber: "p2"),
            () => Run("inProgress", buildNumber: "p3"),
            () => Run("inProgress", buildNumber: "p4"));
        var clock = new FakeWaitClock();

        // Act
        var run = await new PipelineRunWaiter(client, clock).WaitAsync(4202, TimeSpan.FromSeconds(25), TestContext.Current.CancellationToken);

        // Assert
        run.Status.ShouldBe("inProgress");
        run.BuildNumber.ShouldBe("p4");
        client.Calls.Count.ShouldBe(4);
        clock.Delays.ShouldBe([Ten, Ten, TimeSpan.FromSeconds(5)]);
    }

    [Fact]
    public async Task WaitAsync_CompletesAtTheDeadline_ReturnsTheCompletedRow()
    {
        // Arrange
        var client = new ScriptedBuildClient(
            () => Run("inProgress"),
            () => Run("inProgress"),
            () => Run("inProgress"),
            () => Run("completed", "succeeded"));

        // Act
        var run = await new PipelineRunWaiter(client, new FakeWaitClock()).WaitAsync(4202, TimeSpan.FromSeconds(25), TestContext.Current.CancellationToken);

        // Assert
        run.Status.ShouldBe("completed");
        client.Calls.Count.ShouldBe(4);
    }

    [Theory]
    [MemberData(nameof(TransientFailures))]
    public async Task WaitAsync_TransientFailure_SkipsThePoll(Exception failure)
    {
        // Arrange
        var completed = Run("completed", "succeeded");
        var client = new ScriptedBuildClient(() => Run("inProgress"), () => throw failure, () => completed);
        var clock = new FakeWaitClock();

        // Act
        var run = await new PipelineRunWaiter(client, clock).WaitAsync(4202, TimeSpan.FromSeconds(600), TestContext.Current.CancellationToken);

        // Assert
        run.ShouldBeSameAs(completed);
        client.Calls.Count.ShouldBe(3);
        clock.Delays.ShouldBe([Ten, Ten]);
    }

    [Fact]
    public async Task WaitAsync_FirstPollTransient_KeepsWaiting()
    {
        // Arrange
        var completed = Run("completed", "succeeded");
        var client = new ScriptedBuildClient(
            () => throw new UnavailableException("The server answered 503 (Service Unavailable)."),
            () => completed);

        // Act
        var run = await new PipelineRunWaiter(client, new FakeWaitClock()).WaitAsync(4202, TimeSpan.FromSeconds(600), TestContext.Current.CancellationToken);

        // Assert
        run.ShouldBeSameAs(completed);
        client.Calls.Count.ShouldBe(2);
    }

    [Fact]
    public async Task WaitAsync_UnavailableWithRetryAfter_WaitsThatLong()
    {
        // Arrange
        var client = new ScriptedBuildClient(
            () => throw new UnavailableException("busy", TimeSpan.FromSeconds(25)),
            () => Run("completed", "succeeded"));
        var clock = new FakeWaitClock();

        // Act
        await new PipelineRunWaiter(client, clock).WaitAsync(4202, TimeSpan.FromSeconds(600), TestContext.Current.CancellationToken);

        // Assert
        clock.Delays[0].ShouldBe(TimeSpan.FromSeconds(25));
    }

    [Fact]
    public async Task WaitAsync_RetryAfterShorterThanInterval_WaitsInterval()
    {
        // Arrange
        var client = new ScriptedBuildClient(
            () => throw new UnavailableException("unavailable", TimeSpan.FromSeconds(3)),
            () => Run("completed", "succeeded"));
        var clock = new FakeWaitClock();

        // Act
        await new PipelineRunWaiter(client, clock).WaitAsync(4202, TimeSpan.FromSeconds(600), TestContext.Current.CancellationToken);

        // Assert
        clock.Delays.ShouldBe([Ten]);
    }

    [Fact]
    public async Task WaitAsync_RetryAfterPastDeadline_StopsAtDeadline()
    {
        // Arrange
        var client = new ScriptedBuildClient(
            () => Run("inProgress"),
            () => throw new UnavailableException("slow down", TimeSpan.FromSeconds(60)),
            () => Run("inProgress", buildNumber: "last"));
        var clock = new FakeWaitClock();

        // Act
        var run = await new PipelineRunWaiter(client, clock).WaitAsync(4202, TimeSpan.FromSeconds(15), TestContext.Current.CancellationToken);

        // Assert
        clock.Delays.ShouldBe([Ten, TimeSpan.FromSeconds(5)]);
        run.BuildNumber.ShouldBe("last");
        client.Calls.Count.ShouldBe(3);
    }

    [Fact]
    public async Task WaitAsync_Denied_ThrowsAtOnce()
    {
        // Arrange
        var client = new ScriptedBuildClient(() => throw new DeniedException("The server answered 401 (Unauthorized)."));
        var clock = new FakeWaitClock();

        // Act
        var act = () => new PipelineRunWaiter(client, clock).WaitAsync(4202, TimeSpan.FromSeconds(600), TestContext.Current.CancellationToken);

        // Assert
        await Should.ThrowAsync<DeniedException>(act);
        client.Calls.Count.ShouldBe(1);
        clock.Delays.ShouldBeEmpty();
    }

    [Theory]
    [MemberData(nameof(OtherFailures))]
    public async Task WaitAsync_FailureThatIsNotUnavailable_ThrowsAtOnce(Exception failure)
    {
        // Arrange
        var client = new ScriptedBuildClient(() => Run("inProgress"), () => throw failure);
        var clock = new FakeWaitClock();

        // Act
        var thrown = await Should.ThrowAsync<QuillException>(
            () => new PipelineRunWaiter(client, clock).WaitAsync(4202, TimeSpan.FromSeconds(600), TestContext.Current.CancellationToken));

        // Assert
        thrown.ShouldBeSameAs(failure);
        client.Calls.Count.ShouldBe(2);
        clock.Delays.ShouldBe([Ten]);
    }

    [Fact]
    public async Task WaitAsync_NoPollSucceeds_ThrowsUnavailable()
    {
        // Arrange
        var client = new ScriptedBuildClient(() => throw new UnavailableException("down"));

        // Act
        var thrown = await Should.ThrowAsync<UnavailableException>(
            () => new PipelineRunWaiter(client, new FakeWaitClock()).WaitAsync(4202, TimeSpan.FromSeconds(20), TestContext.Current.CancellationToken));

        // Assert
        thrown.Message.ShouldBe("No poll of run 4202 succeeded before the timeout: down");
        thrown.InnerException.ShouldBeOfType<UnavailableException>();
        client.Calls.Count.ShouldBe(3);
    }

    [Fact]
    public async Task WaitAsync_CallerCancels_ThrowsOperationCanceledException()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var client = new ScriptedBuildClient(() => throw new OperationCanceledException(cts.Token));

        // Act
        var act = () => new PipelineRunWaiter(client, new FakeWaitClock()).WaitAsync(4202, TimeSpan.FromSeconds(600), cts.Token);

        // Assert
        await Should.ThrowAsync<OperationCanceledException>(act);
        client.Calls.Count.ShouldBe(1);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task WaitAsync_TimeoutZeroOrNegative_Throws(int seconds)
    {
        // Arrange
        var client = new ScriptedBuildClient(() => Run("completed", "succeeded"));

        // Act
        var act = () => new PipelineRunWaiter(client, new FakeWaitClock()).WaitAsync(4202, TimeSpan.FromSeconds(seconds), TestContext.Current.CancellationToken);

        // Assert
        await Should.ThrowAsync<ArgumentOutOfRangeException>(act);
        client.Calls.Count.ShouldBe(0);
    }

    private static PipelineRun Run(string status, string? result = null, string buildNumber = "20260101.1") => new()
    {
        Id = 4202,
        PipelineId = 17,
        PipelineName = "Contoso.Build",
        BuildNumber = buildNumber,
        Status = status,
        Result = result,
        Reason = "manual",
        SourceBranch = "refs/heads/main",
        SourceVersion = "0123456789abcdef0123456789abcdef01234567",
        RequestedFor = "Jane Doe",
        QueueTime = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
        WebUrl = "https://dev.contoso.example/Project/_build/results?buildId=4202",
    };
}
