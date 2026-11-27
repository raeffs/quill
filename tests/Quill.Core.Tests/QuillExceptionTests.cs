using Shouldly;

namespace Quill.Core.Tests;

public class QuillExceptionTests
{
    [Theory]
    [InlineData(typeof(RejectedException), 1)]
    [InlineData(typeof(UnavailableException), 2)]
    [InlineData(typeof(UsageException), 3)]
    [InlineData(typeof(DefectException), 4)]
    [InlineData(typeof(DeniedException), 5)]
    public void WithMessage_AnyFailureMode_KeepsTheModeAndHoldsTheFirstFailure(Type type, int exitCode)
    {
        var failure = (QuillException)Activator.CreateInstance(type, "The cause.")!;

        var copy = failure.WithMessage("A new message.");

        copy.ShouldBeOfType(type);
        copy.ShouldNotBeSameAs(failure);
        copy.ExitCode.ShouldBe(exitCode);
        copy.Message.ShouldBe("A new message.");
        copy.InnerException.ShouldBeSameAs(failure);
    }

    [Fact]
    public void WithMessage_Unavailable_KeepsRetryAfter()
    {
        var failure = new UnavailableException("The server is busy.", TimeSpan.FromSeconds(30));

        var copy = failure.WithMessage("A new message.").ShouldBeOfType<UnavailableException>();

        copy.RetryAfter.ShouldBe(TimeSpan.FromSeconds(30));
    }
}
