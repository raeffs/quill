using System.Text.Json.Serialization;

namespace Quill.Core.Models;

public class PullRequestVoteCountsResult
{
    [JsonPropertyName("approved")]
    public required int Approved { get; init; }

    [JsonPropertyName("waitingForAuthor")]
    public required int WaitingForAuthor { get; init; }

    [JsonPropertyName("rejected")]
    public required int Rejected { get; init; }

    [JsonPropertyName("noVote")]
    public required int NoVote { get; init; }
}
