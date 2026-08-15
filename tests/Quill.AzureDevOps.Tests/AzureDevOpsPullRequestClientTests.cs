using System.Net;
using Quill.Core.Models;
using Quill.Tests.Shared;
using Shouldly;

namespace Quill.AzureDevOps.Tests;

public class AzureDevOpsPullRequestClientTests
{
    private const string EmptyEnvelope = """{"value":[],"count":0}""";

    [Fact]
    public async Task ListAsync_ProjectWideNoRepo_UsesProjectEndpointAndDefaultStatus()
    {
        // Arrange
        using var handler = new FakeHttpHandler(EmptyEnvelope, HttpStatusCode.OK);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsPullRequestClient(
            httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        // Act
        var result = await client.ListAsync(
            new PullRequestListQuery
            {
                CreatorId = null,
                ReviewerId = null,
                Status = "active",
                Repo = null,
                Top = 50,
            },
            cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBeEmpty();
        var url = handler.LastRequest!.RequestUri!.ToString();
        url.ShouldStartWith($"{TestConstants.ServerUrl}/{TestConstants.Collection}/{TestConstants.Project}/_apis/git/pullrequests?");
        url.ShouldContain("searchCriteria.status=active");
        url.ShouldContain("$top=50");
        url.ShouldNotContain("searchCriteria.creatorId");
        url.ShouldNotContain("searchCriteria.reviewerId");
    }

    [Fact]
    public async Task ListAsync_WithRepo_SwitchesToRepoScopedEndpoint()
    {
        // Arrange
        using var handler = new FakeHttpHandler(EmptyEnvelope, HttpStatusCode.OK);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsPullRequestClient(
            httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        // Act
        await client.ListAsync(
            new PullRequestListQuery
            {
                CreatorId = null,
                ReviewerId = null,
                Status = "active",
                Repo = "importer",
                Top = 50,
            },
            cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        var url = handler.LastRequest!.RequestUri!.ToString();
        url.ShouldStartWith($"{TestConstants.ServerUrl}/{TestConstants.Collection}/{TestConstants.Project}/_apis/git/repositories/importer/pullrequests?");
    }

    [Fact]
    public async Task ListAsync_WithFilters_AppendsCreatorAndReviewerAndStatus()
    {
        // Arrange
        using var handler = new FakeHttpHandler(EmptyEnvelope, HttpStatusCode.OK);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsPullRequestClient(
            httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        // Act
        await client.ListAsync(
            new PullRequestListQuery
            {
                CreatorId = "user-1",
                ReviewerId = "user-2",
                Status = "completed",
                Repo = null,
                Top = 10,
            },
            cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        var url = handler.LastRequest!.RequestUri!.ToString();
        url.ShouldContain("searchCriteria.status=completed");
        url.ShouldContain("searchCriteria.creatorId=user-1");
        url.ShouldContain("searchCriteria.reviewerId=user-2");
        url.ShouldContain("$top=10");
    }

    [Theory]
    [InlineData("feat/retry", "refs%2Fheads%2Ffeat%2Fretry")]
    [InlineData("refs/heads/feat/retry", "refs%2Fheads%2Ffeat%2Fretry")]
    [InlineData("refs/pull/42/merge", "refs%2Fpull%2F42%2Fmerge")]
    public async Task ListAsync_SourceBranch_SendsItAsAFullRef(string given, string expected)
    {
        // Arrange
        using var handler = new FakeHttpHandler(EmptyEnvelope, HttpStatusCode.OK);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsPullRequestClient(
            httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        // Act
        await client.ListAsync(
            new PullRequestListQuery
            {
                Status = "active",
                Top = 50,
                SourceBranch = given,
            },
            cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        var url = handler.LastRequest!.RequestUri!.ToString();
        url.ShouldContain($"searchCriteria.sourceRefName={expected}");
    }

    [Theory]
    [InlineData("main", "refs%2Fheads%2Fmain")]
    [InlineData("refs/heads/main", "refs%2Fheads%2Fmain")]
    public async Task ListAsync_TargetBranch_SendsItAsAFullRef(string given, string expected)
    {
        // Arrange
        using var handler = new FakeHttpHandler(EmptyEnvelope, HttpStatusCode.OK);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsPullRequestClient(
            httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        // Act
        await client.ListAsync(
            new PullRequestListQuery
            {
                Status = "active",
                Top = 50,
                TargetBranch = given,
            },
            cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        var url = handler.LastRequest!.RequestUri!.ToString();
        url.ShouldContain($"searchCriteria.targetRefName={expected}");
    }

    [Fact]
    public async Task ListAsync_NoBranchesAndNoSkip_SendsNoneOfThoseParameters()
    {
        // Arrange
        using var handler = new FakeHttpHandler(EmptyEnvelope, HttpStatusCode.OK);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsPullRequestClient(
            httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        // Act
        await client.ListAsync(
            new PullRequestListQuery { Status = "active", Top = 50 },
            cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        var url = handler.LastRequest!.RequestUri!.ToString();
        url.ShouldNotContain("searchCriteria.sourceRefName");
        url.ShouldNotContain("searchCriteria.targetRefName");
        url.ShouldNotContain("$skip");
    }

    [Fact]
    public async Task ListAsync_Skip_SendsItAsDollarSkip()
    {
        // Arrange
        using var handler = new FakeHttpHandler(EmptyEnvelope, HttpStatusCode.OK);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsPullRequestClient(
            httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        // Act
        await client.ListAsync(
            new PullRequestListQuery { Status = "active", Top = 50, Skip = 100 },
            cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        var url = handler.LastRequest!.RequestUri!.ToString();
        url.ShouldContain("$skip=100");
    }

    [Fact]
    public async Task ListAsync_NegativeSkip_Throws()
    {
        // Arrange
        using var handler = new FakeHttpHandler(EmptyEnvelope, HttpStatusCode.OK);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsPullRequestClient(
            httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        // Act
        Func<Task> act = () => client.ListAsync(
            new PullRequestListQuery { Status = "active", Top = 50, Skip = -1 },
            cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        await act.ShouldThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task ListAsync_MapsAllFieldsAndStripsRefsHeads()
    {
        // Arrange
        var responseJson = $$"""
        {
            "value": [
                {
                    "pullRequestId": 4711,
                    "title": "Fix retry policy in importer",
                    "status": "active",
                    "isDraft": false,
                    "sourceRefName": "refs/heads/feat/retry",
                    "targetRefName": "refs/heads/main",
                    "creationDate": "2026-05-12T08:00:00Z",
                    "createdBy": { "id": "u1", "displayName": "Jane Doe" },
                    "repository": { "id": "r1", "name": "importer" },
                    "reviewers": [
                        { "id": "u2", "displayName": "John Roe", "vote": 0, "isRequired": true },
                        { "id": "u1", "displayName": "Jane Doe", "vote": 10, "isRequired": false }
                    ]
                }
            ],
            "count": 1
        }
        """;

        using var handler = new FakeHttpHandler(responseJson, HttpStatusCode.OK);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsPullRequestClient(
            httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        // Act
        var result = await client.ListAsync(
            new PullRequestListQuery
            {
                CreatorId = null,
                ReviewerId = null,
                Status = "active",
                Repo = null,
                Top = 50,
            },
            cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        var pr = result.ShouldHaveSingleItem();
        pr.Id.ShouldBe(4711);
        pr.Title.ShouldBe("Fix retry policy in importer");
        pr.AuthorDisplayName.ShouldBe("Jane Doe");
        pr.Status.ShouldBe("active");
        pr.IsDraft.ShouldBeFalse();
        pr.RepoName.ShouldBe("importer");
        pr.SourceBranch.ShouldBe("feat/retry");
        pr.TargetBranch.ShouldBe("main");
        pr.ClosedDate.ShouldBeNull();
        pr.WebUrl.ShouldBe($"{TestConstants.ServerUrl}/{TestConstants.Collection}/{TestConstants.Project}/_git/importer/pullrequest/4711");
        pr.Reviewers.Count.ShouldBe(2);
        pr.Reviewers[0].DisplayName.ShouldBe("John Roe");
        pr.Reviewers[0].Vote.ShouldBe(0);
        pr.Reviewers[0].IsRequired.ShouldBeTrue();
        pr.Reviewers[1].DisplayName.ShouldBe("Jane Doe");
        pr.Reviewers[1].Vote.ShouldBe(10);
    }

    [Fact]
    public async Task ListAsync_MapsMergeStatusActiveLabelsAndContainerReviewers()
    {
        // Arrange
        var responseJson = $$"""
        {
            "value": [
                {
                    "pullRequestId": 4711,
                    "title": "Fix retry policy in importer",
                    "status": "active",
                    "isDraft": false,
                    "mergeStatus": "conflicts",
                    "sourceRefName": "refs/heads/feat/retry",
                    "targetRefName": "refs/heads/main",
                    "creationDate": "2026-05-12T08:00:00Z",
                    "createdBy": { "id": "u1", "displayName": "Jane Doe" },
                    "repository": { "id": "r1", "name": "importer" },
                    "labels": [
                        { "id": "l1", "name": "needs-docs", "active": true },
                        { "id": "l2", "name": "retired", "active": false }
                    ],
                    "reviewers": [
                        { "id": "g1", "displayName": "Importer Team", "vote": 0, "isRequired": true, "isContainer": true },
                        { "id": "u2", "displayName": "John Roe", "vote": 10, "isRequired": true }
                    ]
                }
            ],
            "count": 1
        }
        """;

        using var handler = new FakeHttpHandler(responseJson, HttpStatusCode.OK);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsPullRequestClient(
            httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        // Act
        var result = await client.ListAsync(
            new PullRequestListQuery
            {
                CreatorId = null,
                ReviewerId = null,
                Status = "active",
                Repo = null,
                Top = 50,
            },
            cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        var pr = result.ShouldHaveSingleItem();
        pr.MergeStatus.ShouldBe("conflicts");
        pr.Labels.ShouldBe(["needs-docs"]);
        pr.Reviewers[0].IsContainer.ShouldBeTrue();
        pr.Reviewers[1].IsContainer.ShouldBeFalse();
    }

    [Fact]
    public async Task ListAsync_NoMergeStatusOrLabels_LeavesThemNullAndEmpty()
    {
        // Arrange
        var responseJson = $$"""
        {
            "value": [
                {
                    "pullRequestId": 4711,
                    "title": "Fix retry policy in importer",
                    "status": "active",
                    "isDraft": false,
                    "sourceRefName": "refs/heads/feat/retry",
                    "targetRefName": "refs/heads/main",
                    "creationDate": "2026-05-12T08:00:00Z",
                    "createdBy": { "id": "u1", "displayName": "Jane Doe" },
                    "repository": { "id": "r1", "name": "importer" }
                }
            ],
            "count": 1
        }
        """;

        using var handler = new FakeHttpHandler(responseJson, HttpStatusCode.OK);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsPullRequestClient(
            httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        // Act
        var result = await client.ListAsync(
            new PullRequestListQuery
            {
                CreatorId = null,
                ReviewerId = null,
                Status = "active",
                Repo = null,
                Top = 50,
            },
            cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        var pr = result.ShouldHaveSingleItem();
        pr.MergeStatus.ShouldBeNull();
        pr.Labels.ShouldBeEmpty();
        pr.Reviewers.ShouldBeEmpty();
    }

    [Fact]
    public async Task ListAsync_DraftInResponse_ReturnsItMarkedAsDraft()
    {
        // Arrange
        var responseJson = $$"""
        {
            "value": [
                {
                    "pullRequestId": 4711,
                    "title": "Work in progress",
                    "status": "active",
                    "isDraft": true,
                    "sourceRefName": "refs/heads/feat/retry",
                    "targetRefName": "refs/heads/main",
                    "creationDate": "2026-05-12T08:00:00Z",
                    "createdBy": { "id": "u1", "displayName": "Jane Doe" },
                    "repository": { "id": "r1", "name": "importer" },
                    "reviewers": []
                },
                {
                    "pullRequestId": 4712,
                    "title": "Ready for review",
                    "status": "active",
                    "isDraft": false,
                    "sourceRefName": "refs/heads/feat/timeout",
                    "targetRefName": "refs/heads/main",
                    "creationDate": "2026-05-12T09:00:00Z",
                    "createdBy": { "id": "u1", "displayName": "Jane Doe" },
                    "repository": { "id": "r1", "name": "importer" },
                    "reviewers": []
                }
            ],
            "count": 2
        }
        """;

        using var handler = new FakeHttpHandler(responseJson, HttpStatusCode.OK);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsPullRequestClient(
            httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        // Act
        var result = await client.ListAsync(
            new PullRequestListQuery
            {
                CreatorId = null,
                ReviewerId = null,
                Status = "active",
                Repo = null,
                Top = 50,
            },
            cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.Count.ShouldBe(2);
        result[0].Id.ShouldBe(4711);
        result[0].IsDraft.ShouldBeTrue();
        result[1].Id.ShouldBe(4712);
        result[1].IsDraft.ShouldBeFalse();
    }

    [Fact]
    public async Task ListAsync_Non2xx_ThrowsHttpRequestException()
    {
        // Arrange
        using var handler = new FakeHttpHandler("""{"message":"Unauthorized"}""", HttpStatusCode.Unauthorized);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsPullRequestClient(
            httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        // Act
        Func<Task> act = () => client.ListAsync(
            new PullRequestListQuery
            {
                CreatorId = null,
                ReviewerId = null,
                Status = "active",
                Repo = null,
                Top = 50,
            },
            cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        await Should.ThrowAsync<HttpRequestException>(act);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task ListAsync_InvalidTop_ThrowsArgumentOutOfRangeException(int top)
    {
        // Arrange
        using var handler = new FakeHttpHandler(EmptyEnvelope, HttpStatusCode.OK);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsPullRequestClient(
            httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        // Act
        Func<Task> act = () => client.ListAsync(
            new PullRequestListQuery
            {
                CreatorId = null,
                ReviewerId = null,
                Status = "active",
                Repo = null,
                Top = top,
            },
            cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        await Should.ThrowAsync<ArgumentOutOfRangeException>(act);
    }
}
