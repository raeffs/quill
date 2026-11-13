using Quill.Core;
using Shouldly;

namespace Quill.Cli.Tests;

[Collection(nameof(ProcessStateCollection))]
public sealed class HandleErrorTests : IDisposable
{
#pragma warning disable CA2213 // The process owns the original Console.Out; this class only puts it back
    private readonly TextWriter _originalOut = Console.Out;
#pragma warning restore CA2213
    private readonly StringWriter _output = new();

    public HandleErrorTests() => Console.SetOut(_output);

    public void Dispose()
    {
        Console.SetOut(_originalOut);
        Environment.ExitCode = 0;
        _output.Dispose();
    }

    [Fact]
    public void HandleError_InvalidRequest_Returns1WithTheMessage()
    {
        CommandHelpers.HandleError(new InvalidRequestException("Frontmatter must contain a 'type' field."));

        Environment.ExitCode.ShouldBe(1);
        _output.ToString().ShouldBe("{\"error\":\"Frontmatter must contain a 'type' field.\",\"code\":1}" + Environment.NewLine);
    }
}
