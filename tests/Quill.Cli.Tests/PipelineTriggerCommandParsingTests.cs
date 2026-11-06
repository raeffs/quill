using Shouldly;

namespace Quill.Cli.Tests;

public class PipelineTriggerCommandParsingTests
{
    private static readonly string[] Prefix = ["pipeline", "trigger", "42"];

    [Fact]
    public void Parse_PipelineIdOnly_NoErrors()
    {
        // Arrange
        // Act
        var parseResult = CliHost.Parse(Prefix, TestServices.Empty);

        // Assert
        parseResult.Errors.ShouldBeEmpty();
        parseResult.GetValue<int>("pipelineId").ShouldBe(42);
        parseResult.GetValue<string?>("--branch").ShouldBeNull();
        (parseResult.GetValue<string[]>("--parameter") ?? []).ShouldBeEmpty();
        (parseResult.GetValue<string[]>("--skip-stage") ?? []).ShouldBeEmpty();
    }

    [Fact]
    public void Parse_RepeatableFlags_CollectEveryValueInOrder()
    {
        // Arrange
        string[] args =
        [
            .. Prefix,
            "--branch", "feature/login",
            "--parameter", "env=dev", "--parameter", "filter=a=b", "--parameter", "empty=",
            "--skip-stage", "Test", "--skip-stage", "Deploy",
        ];

        // Act
        var parseResult = CliHost.Parse(args, TestServices.Empty);

        // Assert
        parseResult.Errors.ShouldBeEmpty();
        parseResult.GetValue<string?>("--branch").ShouldBe("feature/login");
        parseResult.GetValue<string[]>("--parameter").ShouldBe(["env=dev", "filter=a=b", "empty="]);
        parseResult.GetValue<string[]>("--skip-stage").ShouldBe(["Test", "Deploy"]);
    }

    [Theory]
    [InlineData("pipeline", "trigger")]
    [InlineData("pipeline", "trigger", "abc")]
    [InlineData("pipeline", "trigger", "0")]
    [InlineData("pipeline", "trigger", "-1")]
    public void Parse_BadPipelineId_ProducesError(params string[] args)
    {
        // Arrange
        // Act
        var parseResult = CliHost.Parse(args, TestServices.Empty);

        // Assert
        parseResult.Errors.ShouldNotBeEmpty();
    }

    [Theory]
    [InlineData("--parameter", "noequals")]
    [InlineData("--parameter", "=value")]
    [InlineData("--parameter", "env=dev", "--parameter", "env=prod")]
    [InlineData("--branch", "")]
    [InlineData("--skip-stage", "")]
    [InlineData("--variable", "x=1")]
    [InlineData("--wait")]
    public void Parse_BadFlags_ProducesError(params string[] flags)
    {
        // Arrange
        // Act
        var parseResult = CliHost.Parse([.. Prefix, .. flags], TestServices.Empty);

        // Assert
        parseResult.Errors.ShouldNotBeEmpty();
    }
}
