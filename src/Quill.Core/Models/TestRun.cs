namespace Quill.Core.Models;

public class TestRun
{
    public required int Id { get; init; }

    public required string Name { get; init; }

    public required string State { get; init; }

    public int TotalTests { get; init; }

    public int PassedTests { get; init; }

    public int UnanalyzedTests { get; init; }

    public int NotApplicableTests { get; init; }
}
