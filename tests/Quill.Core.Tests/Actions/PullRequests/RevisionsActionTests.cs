using Quill.Core.Actions.PullRequests;
using Quill.Core.Models;
using Quill.Tests.Shared.Fakes;
using Shouldly;

namespace Quill.Core.Tests.Actions.PullRequests;

public class RevisionsActionTests
{
    [Fact]
    public async Task RunAsync_ReadsThePullRequestThenItsRevisionsInTheRepo()
    {
        var client = new ScriptedPullRequestClient
        {
            GetById = id => PullRequest(id),
            GetRevisions = (_, _) => [Revision(2), Revision(1)],
        };

        var rows = await RevisionsAction.RunAsync(client, 12, TestContext.Current.CancellationToken);

        rows.Select(row => row.Id).ShouldBe([2, 1]);
        client.Calls.ShouldBe(["GetById(12)", "GetRevisions(12,repo)"]);
    }

    private static PullRequest PullRequest(int id) => new()
    {
        Id = id,
        Title = "Add feature",
        AuthorDisplayName = "Jane Doe",
        Status = "active",
        IsDraft = false,
        RepoName = "repo",
        SourceBranch = "refs/heads/feature",
        TargetBranch = "refs/heads/main",
        CreatedDate = new DateTimeOffset(2026, 1, 1, 9, 0, 0, TimeSpan.Zero),
        Reviewers = [],
        WebUrl = "https://dev.example.com/Contoso/_git/repo/pullrequest/12",
    };

    private static PullRequestRevision Revision(int id) => new()
    {
        Id = id,
        CreatedDate = new DateTimeOffset(2026, 1, 1, 9, 0, 0, TimeSpan.Zero),
        Author = "Jane Doe",
    };
}
