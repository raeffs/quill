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
            Name = "Contoso PR Validation",
            Path = "\\",
        };

        // Act
        var result = PipelineResultBuilder.Build(pipeline);

        // Assert
        result.Id.ShouldBe(42);
        result.Name.ShouldBe("Contoso PR Validation");
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
            Name = "Contoso PR Validation",
            Path = path,
        };

        // Act
        var result = PipelineResultBuilder.Build(pipeline);

        // Assert
        result.Folder.ShouldBe(path);
    }
}
