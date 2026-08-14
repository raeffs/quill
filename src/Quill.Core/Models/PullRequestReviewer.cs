namespace Quill.Core.Models;

public class PullRequestReviewer
{
    public required string Id { get; init; }

    public required string DisplayName { get; init; }

    public required int Vote { get; init; }

    public required bool IsRequired { get; init; }
}
