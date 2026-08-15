using System.Net;
using Quill.Tests.Shared;
using Shouldly;

namespace Quill.AzureDevOps.Tests;

public class AzureDevOpsPullRequestClientMergeAttemptTests
{
    [Fact]
    public async Task GetByIdAsync_KeepsTheCommitIdsOfTheMergeAttempt()
    {
        // Arrange
        var responseJson = """
        {
            "pullRequestId": 4711,
            "title": "Fix retry policy",
            "status": "active",
            "isDraft": false,
            "mergeStatus": "conflicts",
            "sourceRefName": "refs/heads/feat/retry",
            "targetRefName": "refs/heads/main",
            "creationDate": "2026-05-12T08:00:00Z",
            "createdBy": { "id": "u1", "displayName": "Jane Doe" },
            "repository": { "id": "r1", "name": "importer" },
            "reviewers": [],
            "lastMergeSourceCommit": {
                "commitId": "1111111111111111111111111111111111111111",
                "url": "https://example.invalid/commits/1111111111111111111111111111111111111111"
            },
            "lastMergeTargetCommit": {
                "commitId": "2222222222222222222222222222222222222222",
                "url": "https://example.invalid/commits/2222222222222222222222222222222222222222"
            }
        }
        """;
        using var handler = new FakeHttpHandler(responseJson, HttpStatusCode.OK);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsPullRequestClient(
            httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        // Act
        var pr = await client.GetByIdAsync(4711, TestContext.Current.CancellationToken);

        // Assert
        pr.LastMergeSourceCommit.ShouldBe("1111111111111111111111111111111111111111");
        pr.LastMergeTargetCommit.ShouldBe("2222222222222222222222222222222222222222");
    }

    [Fact]
    public async Task GetByIdAsync_NoMergeAttemptInResponse_LeavesBothCommitsNull()
    {
        // Arrange
        var responseJson = """
        {
            "pullRequestId": 4711,
            "title": "Fix retry policy",
            "status": "active",
            "isDraft": false,
            "sourceRefName": "refs/heads/feat/retry",
            "targetRefName": "refs/heads/main",
            "creationDate": "2026-05-12T08:00:00Z",
            "createdBy": { "id": "u1", "displayName": "Jane Doe" },
            "repository": { "id": "r1", "name": "importer" },
            "reviewers": []
        }
        """;
        using var handler = new FakeHttpHandler(responseJson, HttpStatusCode.OK);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsPullRequestClient(
            httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        // Act
        var pr = await client.GetByIdAsync(4711, TestContext.Current.CancellationToken);

        // Assert
        pr.LastMergeSourceCommit.ShouldBeNull();
        pr.LastMergeTargetCommit.ShouldBeNull();
    }
}
