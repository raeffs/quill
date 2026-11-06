using Quill.Core;
using Quill.Core.Models;
using Shouldly;

namespace Quill.Core.Tests;

public class PipelineTestRunResultBuilderTests
{
    [Fact]
    public void Build_MapsTheCountsToTheRowKeys()
    {
        // Arrange
        var run = MakeRun();

        // Act
        var result = PipelineTestRunResultBuilder.Build(run, null);

        // Assert
        result.Id.ShouldBe(9001);
        result.Name.ShouldBe("VSTest_TestResults_4201");
        result.State.ShouldBe("Completed");
        result.Total.ShouldBe(120);
        result.Passed.ShouldBe(110);
        result.Failed.ShouldBe(7);
        result.Skipped.ShouldBe(3);
    }

    [Fact]
    public void Build_NoResults_LeavesResultsNull()
    {
        // Act
        var result = PipelineTestRunResultBuilder.Build(MakeRun(), null);

        // Assert
        result.Results.ShouldBeNull();
    }

    [Fact]
    public void Build_EmptyResults_KeepsEmptyList()
    {
        // Act
        var result = PipelineTestRunResultBuilder.Build(MakeRun(), []);

        // Assert
        result.Results.ShouldNotBeNull();
        result.Results.ShouldBeEmpty();
    }

    [Fact]
    public void Build_FailedResult_MapsEveryKey()
    {
        // Arrange
        var failed = new TestCaseResult
        {
            Title = "Contoso.Tests.OrderTests.Total_IsSum(order: Order { … })",
            Name = "Contoso.Tests.OrderTests.Total_IsSum",
            Storage = "contoso.tests.dll",
            Outcome = "Failed",
            Message = "Expected 3, found 4.",
            StackTrace = "   at Contoso.Tests.OrderTests.Total_IsSum()",
            DurationInMs = 12,
            FailingSince = new DateTimeOffset(2026, 11, 6, 10, 5, 36, 480, TimeSpan.Zero),
            FailingSinceRunId = 4199,
        };

        // Act
        var result = PipelineTestRunResultBuilder.Build(MakeRun(), [failed]);

        // Assert
        var row = result.Results.ShouldHaveSingleItem();
        row.Title.ShouldBe("Contoso.Tests.OrderTests.Total_IsSum(order: Order { … })");
        row.Name.ShouldBe("Contoso.Tests.OrderTests.Total_IsSum");
        row.Storage.ShouldBe("contoso.tests.dll");
        row.Outcome.ShouldBe("Failed");
        row.Message.ShouldBe("Expected 3, found 4.");
        row.StackTrace.ShouldBe("   at Contoso.Tests.OrderTests.Total_IsSum()");
        row.DurationMs.ShouldBe(12);
        row.FailingSince.ShouldBe("2026-11-06T10:05:36Z");
        row.FailingSinceRunId.ShouldBe(4199);
    }

    [Theory]
    [InlineData(2.5, 3)]
    [InlineData(2.4, 2)]
    [InlineData(0.5, 1)]
    [InlineData(0.0, 0)]
    public void Build_DurationInMs_RoundsToInteger(double server, int expected)
    {
        // Arrange
        var timed = new TestCaseResult { Outcome = "Passed", DurationInMs = server };

        // Act
        var result = PipelineTestRunResultBuilder.Build(MakeRun(), [timed]);

        // Assert
        result.Results.ShouldHaveSingleItem().DurationMs.ShouldBe(expected);
    }

    [Fact]
    public void Build_PassedResultWithoutFailureFields_LeavesThemNull()
    {
        // Arrange
        var passed = new TestCaseResult { Outcome = "Passed" };

        // Act
        var result = PipelineTestRunResultBuilder.Build(MakeRun(), [passed]);

        // Assert
        var row = result.Results.ShouldHaveSingleItem();
        row.Message.ShouldBeNull();
        row.StackTrace.ShouldBeNull();
        row.DurationMs.ShouldBeNull();
        row.FailingSince.ShouldBeNull();
        row.FailingSinceRunId.ShouldBeNull();
    }

    [Fact]
    public void Build_KeepsTheResultOrder()
    {
        // Arrange
        TestCaseResult[] results =
        [
            new() { Name = "a", Outcome = "Passed" },
            new() { Name = "c", Outcome = "Passed" },
            new() { Name = "b", Outcome = "Passed" },
        ];

        // Act
        var result = PipelineTestRunResultBuilder.Build(MakeRun(), results);

        // Assert
        result.Results!.Select(r => r.Name).ShouldBe(["a", "c", "b"]);
    }

    private static TestRun MakeRun()
    {
        return new TestRun
        {
            Id = 9001,
            Name = "VSTest_TestResults_4201",
            State = "Completed",
            TotalTests = 120,
            PassedTests = 110,
            UnanalyzedTests = 7,
            NotApplicableTests = 3,
        };
    }
}
