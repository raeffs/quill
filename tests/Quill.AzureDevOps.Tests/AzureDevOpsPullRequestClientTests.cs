using System.Net;
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
            creatorId: null,
            reviewerId: null,
            status: "active",
            repo: null,
            top: 50,
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
            creatorId: null,
            reviewerId: null,
            status: "active",
            repo: "importer",
            top: 50,
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
            creatorId: "user-1",
            reviewerId: "user-2",
            status: "completed",
            repo: null,
            top: 10,
            cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        var url = handler.LastRequest!.RequestUri!.ToString();
        url.ShouldContain("searchCriteria.status=completed");
        url.ShouldContain("searchCriteria.creatorId=user-1");
        url.ShouldContain("searchCriteria.reviewerId=user-2");
        url.ShouldContain("$top=10");
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
            creatorId: null,
            reviewerId: null,
            status: "active",
            repo: null,
            top: 50,
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
    public async Task ListAsync_Non2xx_ThrowsHttpRequestException()
    {
        // Arrange
        using var handler = new FakeHttpHandler("""{"message":"Unauthorized"}""", HttpStatusCode.Unauthorized);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestConstants.ServerUrl) };
        var client = new AzureDevOpsPullRequestClient(
            httpClient, TestConstants.ServerUrl, TestConstants.Collection, TestConstants.Project);

        // Act
        Func<Task> act = () => client.ListAsync(
            creatorId: null,
            reviewerId: null,
            status: "active",
            repo: null,
            top: 50,
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
            creatorId: null,
            reviewerId: null,
            status: "active",
            repo: null,
            top: top,
            cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        await Should.ThrowAsync<ArgumentOutOfRangeException>(act);
    }
}
