using Shouldly;

namespace Quill.Core.Tests;

public class LaterStepTests
{
    [Theory]
    [InlineData(typeof(RejectedException))]
    [InlineData(typeof(UnavailableException))]
    [InlineData(typeof(UsageException))]
    [InlineData(typeof(DefectException))]
    [InlineData(typeof(DeniedException))]
    public async Task RunAsync_StepFails_KeepsTheFailureModeAndStartsWithWhatQuillWrote(Type type)
    {
        var failure = (QuillException)Activator.CreateInstance(type, "The cause.")!;

        var ex = await Should.ThrowAsync<QuillException>(
            () => LaterStep.RunAsync<int>("Created work item 7", "linking it", () => throw failure));

        ex.ShouldBeOfType(type);
        ex.Message.ShouldBe("Created work item 7, but linking it failed: The cause.");
        ex.InnerException.ShouldBeSameAs(failure);
    }

    [Fact]
    public async Task RunAsync_StepWithoutAResultFails_StartsWithWhatQuillWrote()
    {
        Func<Task> step = () => throw new RejectedException("The cause.");

        var ex = await Should.ThrowAsync<RejectedException>(
            () => LaterStep.RunAsync("Queued run 9", "reading it", step));

        ex.Message.ShouldBe("Queued run 9, but reading it failed: The cause.");
    }

    [Fact]
    public async Task RunAsync_StepSucceeds_ReturnsItsResult()
    {
        var result = await LaterStep.RunAsync("Created work item 7", "reading it", () => Task.FromResult(42));

        result.ShouldBe(42);
    }

    [Fact]
    public async Task RunAsync_ExceptionWithoutAFailureMode_PassesThroughUnchanged()
    {
        var exception = new InvalidOperationException("boom");

        var ex = await Should.ThrowAsync<InvalidOperationException>(
            () => LaterStep.RunAsync<int>("Created work item 7", "linking it", () => throw exception));

        ex.ShouldBeSameAs(exception);
    }

    [Fact]
    public async Task RunAsync_StepCancelled_StaysACancellation()
    {
        await Should.ThrowAsync<OperationCanceledException>(
            () => LaterStep.RunAsync<int>("Created work item 7", "linking it", () => throw new OperationCanceledException("boom")));
    }
}
