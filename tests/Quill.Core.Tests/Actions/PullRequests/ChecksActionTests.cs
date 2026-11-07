using Quill.Core.Actions.PullRequests;
using Quill.Core.Models;
using Quill.Tests.Shared.Fakes;
using Shouldly;

namespace Quill.Core.Tests.Actions.PullRequests;

public class ChecksActionTests
{
    [Fact]
    public async Task RunAsync_ReadsThePullRequestThenItsEvaluations()
    {
        var client = new ScriptedPullRequestClient
        {
            GetById = id => PullRequest(id, projectId: "proj", codeReviewId: 99),
            GetPolicyEvaluations = (_, _) => [Evaluation("Build")],
        };

        var row = await ChecksAction.RunAsync(client, 12, TestContext.Current.CancellationToken);

        row.Checks.Single().Name.ShouldBe("Build");
        client.Calls.ShouldBe(["GetById(12)", "GetPolicyEvaluations(proj,99)"]);
    }

    [Fact]
    public async Task RunAsync_PullRequestWithoutProject_ThrowsBeforeTheSecondRequest()
    {
        var client = new ScriptedPullRequestClient { GetById = id => PullRequest(id, projectId: string.Empty, codeReviewId: 99) };

        await Should.ThrowAsync<InvalidOperationException>(() => ChecksAction.RunAsync(client, 12, TestContext.Current.CancellationToken));

        client.Calls.ShouldBe(["GetById(12)"]);
    }

    private static PullRequest PullRequest(int id, string projectId, int codeReviewId) => new()
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
        ProjectId = projectId,
        CodeReviewId = codeReviewId,
    };

    private static PolicyEvaluation Evaluation(string name) => new()
    {
        Name = name,
        Status = "approved",
    };
}
