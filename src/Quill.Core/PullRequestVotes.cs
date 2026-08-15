using Quill.Core.Models;

namespace Quill.Core;

public static class PullRequestVotes
{
    public const string Approved = "approved";
    public const string ApprovedWithSuggestions = "approvedWithSuggestions";
    public const string WaitingForAuthor = "waitingForAuthor";
    public const string Rejected = "rejected";
    public const string NoVote = "noVote";

    public static string Name(int vote) => vote switch
    {
        10 => Approved,
        5 => ApprovedWithSuggestions,
        -5 => WaitingForAuthor,
        -10 => Rejected,
        _ => NoVote,
    };

    public static PullRequestVoteCountsResult Count(IReadOnlyList<PullRequestReviewer> reviewers)
    {
        int approved = 0;
        int waitingForAuthor = 0;
        int rejected = 0;
        int noVote = 0;

        foreach (var reviewer in reviewers)
        {
            if (reviewer.IsContainer)
            {
                continue;
            }

            switch (reviewer.Vote)
            {
                case 10:
                case 5:
                    approved++;
                    break;
                case -5:
                    waitingForAuthor++;
                    break;
                case -10:
                    rejected++;
                    break;
                default:
                    noVote++;
                    break;
            }
        }

        return new PullRequestVoteCountsResult
        {
            Approved = approved,
            WaitingForAuthor = waitingForAuthor,
            Rejected = rejected,
            NoVote = noVote,
        };
    }
}
