namespace Quill.Core.Models;

public class TestResultQuery
{
    public required IReadOnlyList<string> Outcomes { get; init; }

    public int? Top { get; init; }

    public int? Skip { get; init; }
}
