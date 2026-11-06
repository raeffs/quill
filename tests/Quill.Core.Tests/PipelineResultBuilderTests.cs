using Quill.Core;
using Quill.Core.Models;
using Shouldly;

namespace Quill.Core.Tests;

public class PipelineResultBuilderTests
{
    [Fact]
    public void Build_MapsIdNameAndPathToFolder()
    {
        // Arrange
        var pipeline = new Pipeline
        {
            Id = 42,
            Name = "G6 PR Validation Pipeline",
            Path = "\\",
        };

        // Act
        var result = PipelineResultBuilder.Build(pipeline);

        // Assert
        result.Id.ShouldBe(42);
        result.Name.ShouldBe("G6 PR Validation Pipeline");
        result.Folder.ShouldBe("\\");
    }

    [Theory]
    [InlineData("\\")]
    [InlineData("\\Team A")]
    [InlineData("\\Team A\\CI")]
    public void Build_Path_PassesVerbatimAsFolder(string path)
    {
        // Arrange
        var pipeline = new Pipeline
        {
            Id = 42,
            Name = "G6 PR Validation Pipeline",
            Path = path,
        };

        // Act
        var result = PipelineResultBuilder.Build(pipeline);

        // Assert
        result.Folder.ShouldBe(path);
    }
}
