namespace Quill.Core.Models;

public class PullRequestComment
{
    public required int Id { get; init; }

    public string? Author { get; init; }

    public required DateTimeOffset CreatedDate { get; init; }

    public DateTimeOffset? ModifiedDate { get; init; }

    public required DateTimeOffset LastUpdatedDate { get; init; }

    public required IReadOnlyList<string> UsersLiked { get; init; }

    public required string TextHtml { get; init; }
}
