namespace Quill.Core.Models;

public class WorkItemComment
{
    public required int Id { get; init; }

    public string? Author { get; init; }

    public required DateTimeOffset CreatedDate { get; init; }

    public DateTimeOffset? ModifiedDate { get; init; }

    public required string TextHtml { get; init; }
}
