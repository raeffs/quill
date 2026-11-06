using System.Net;
using System.Text.Json;
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
    public void HandleError_WorkItemRejected_Returns1WithTheMessage()
    {
        CommandHelpers.HandleError(new WorkItemRejectedException("TF401232: cycle (400)"));

        Environment.ExitCode.ShouldBe(1);
        _output.ToString().ShouldContain("TF401232: cycle (400)");
    }

    [Fact]
    public void HandleError_HttpRequestException_Returns2()
    {
        CommandHelpers.HandleError(new HttpRequestException("down"));

        Environment.ExitCode.ShouldBe(2);
    }

    [Fact]
    public void HandleError_Unauthorized_EmitsOnlyErrorAndCode()
    {
        CommandHelpers.HandleError(new HttpRequestException(
            "Response status code does not indicate success: 401 (Unauthorized).", null, HttpStatusCode.Unauthorized));

        using var document = JsonDocument.Parse(_output.ToString());
        document.RootElement.EnumerateObject().Select(p => p.Name).ShouldBe(["error", "code"]);
        document.RootElement.GetProperty("code").GetInt32().ShouldBe(2);
        Environment.ExitCode.ShouldBe(2);
    }

    [Fact]
    public void HandleError_PipelineRejected_Returns1WithTheMessage()
    {
        CommandHelpers.HandleError(new PipelineRejectedException("Unexpected parameter 'colour'. (400)"));

        Environment.ExitCode.ShouldBe(1);
        _output.ToString().ShouldContain("Unexpected parameter 'colour'. (400)");
    }

    [Fact]
    public void HandleError_NotFound_Returns2()
    {
        CommandHelpers.HandleError(new HttpRequestException(
            "Response status code does not indicate success: 404 (Not Found).", null, HttpStatusCode.NotFound));

        Environment.ExitCode.ShouldBe(2);
    }
}
