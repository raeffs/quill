using Quill.Core;
using Shouldly;

namespace Quill.Core.Tests;

public class PipelineLogResultBuilderTests
{
    [Theory]
    [InlineData(1001, 200, 801)]
    [InlineData(1001, 1, 1000)]
    [InlineData(1001, 1000, 1)]
    [InlineData(5, 10, 1)]
    [InlineData(5, 5, 1)]
    [InlineData(0, 3, 1)]
    public void TailStartLine_ReturnsLineCountMinusTailButAtLeastOne(int lineCount, int tail, int expected)
    {
        PipelineLogResultBuilder.TailStartLine(lineCount, tail).ShouldBe(expected);
    }

    [Fact]
    public void Build_NoStartLineSent_NumbersFromOne()
    {
        string[] lines = ["a", "b", "c"];

        var result = PipelineLogResultBuilder.Build(4201, 7, null, lines);

        result.RunId.ShouldBe(4201);
        result.LogId.ShouldBe(7);
        result.StartLine.ShouldBe(1);
        result.EndLine.ShouldBe(3);
        result.Lines.ShouldBe(lines);
    }

    [Fact]
    public void Build_StartLineSent_EndLineIsStartPlusCountMinusOne()
    {
        var lines = Enumerable.Range(0, 200).Select(i => $"line {i}").ToList();

        var result = PipelineLogResultBuilder.Build(4201, 7, 401, lines);

        result.StartLine.ShouldBe(401);
        result.EndLine.ShouldBe(600);
    }

    [Fact]
    public void Build_OneLine_StartAndEndAreEqual()
    {
        var result = PipelineLogResultBuilder.Build(4201, 7, 7, ["x"]);

        result.StartLine.ShouldBe(7);
        result.EndLine.ShouldBe(7);
    }

    [Fact]
    public void Build_NoLines_BothLineKeysNull()
    {
        var result = PipelineLogResultBuilder.Build(4201, 7, 5000, []);

        result.StartLine.ShouldBeNull();
        result.EndLine.ShouldBeNull();
        result.Lines.ShouldBeEmpty();
        result.RunId.ShouldBe(4201);
        result.LogId.ShouldBe(7);
    }

    [Fact]
    public void Build_TailLongerThanLog_PrintsFromLineOne()
    {
        var start = PipelineLogResultBuilder.TailStartLine(5, 10);

        var result = PipelineLogResultBuilder.Build(4201, 7, start, ["a", "b", "c", "d"]);

        result.StartLine.ShouldBe(1);
        result.EndLine.ShouldBe(4);
    }
}
