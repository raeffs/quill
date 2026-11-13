using System.Net;
using Quill.Core;
using Quill.Core.Models;
using Quill.Tests.Shared;
using Shouldly;

namespace Quill.AzureDevOps.Tests;

public class AzureDevOpsIdentityClientTests
{
    private const string PersonJson = """
    {"count":1,"value":[{"id":"a1b2c3d4-0000-4000-8000-000000000001","providerDisplayName":"Jane Doe",
    "properties":{"Domain":{"$type":"System.String","$value":"CONTOSO"},"Account":{"$type":"System.String","$value":"jdoe"}}}]}
    """;

    [Fact]
    public async Task GetCurrentUserAsync_ValidResponse_ReturnsUserIdAndDisplayName()
    {
        // Arrange
        var connectionDataJson = """
        {
            "authenticatedUser": {
                "id": "6f1c2b7e-3d4a-4e5f-9a8b-0c1d2e3f4a5b",
                "providerDisplayName": "Test User"
            }
        }
        """;

        using var handler = new FakeHttpHandler(connectionDataJson, HttpStatusCode.OK);
        using var httpClient = TestHttp.Client(handler);
        var client = new AzureDevOpsIdentityClient(httpClient, TestConstants.ServerUrl, TestConstants.Collection);

        // Act
        var currentUser = await client.GetCurrentUserAsync();

        // Assert
        currentUser.Id.ShouldBe("6f1c2b7e-3d4a-4e5f-9a8b-0c1d2e3f4a5b");
        currentUser.DisplayName.ShouldBe("Test User");
        handler.LastRequest!.RequestUri!.ToString().ShouldBe(
            $"{TestConstants.ServerUrl}/{TestConstants.Collection}/_apis/connectionData");
    }

    [Fact]
    public async Task SearchAsync_General_SendsTheValueUnchangedToTheCollectionUrl()
    {
        using var harness = new Harness(PersonJson);

        await harness.Client.SearchAsync(IdentitySearchFilter.General, @"Jane Doe <CONTOSO\jdoe>", CancellationToken.None);

        var uri = harness.Handler.LastRequest!.RequestUri!;
        uri.GetLeftPart(UriPartial.Path).ShouldBe($"{TestConstants.ServerUrl}/{TestConstants.Collection}/_apis/identities");
        var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
        query["searchFilter"].ShouldBe("General");
        query["filterValue"].ShouldBe(@"Jane Doe <CONTOSO\jdoe>");
        query["queryMembership"].ShouldBe("None");
        query["api-version"].ShouldBe("7.1");
    }

    [Fact]
    public async Task SearchAsync_MailAddress_SendsMailAddressFilter()
    {
        using var harness = new Harness(PersonJson);

        await harness.Client.SearchAsync(IdentitySearchFilter.MailAddress, "jane+test@contoso.com", CancellationToken.None);

        var query = System.Web.HttpUtility.ParseQueryString(harness.Handler.LastRequest!.RequestUri!.Query);
        query["searchFilter"].ShouldBe("MailAddress");
        query["filterValue"].ShouldBe("jane+test@contoso.com");
    }

    [Fact]
    public async Task SearchAsync_Person_ReadsIdNameAndUniqueName()
    {
        using var harness = new Harness(PersonJson);

        var matches = await harness.Client.SearchAsync(IdentitySearchFilter.General, "Jane", CancellationToken.None);

        matches.ShouldBe([new IdentityMatch("a1b2c3d4-0000-4000-8000-000000000001", "Jane Doe", @"CONTOSO\jdoe", IsContainer: false)]);
    }

    [Fact]
    public async Task SearchAsync_Group_IsContainerTrue()
    {
        var json = """
        {"count":1,"value":[{"id":"a1b2c3d4-0000-4000-8000-000000000002","providerDisplayName":"Contoso Team","isContainer":true,
        "properties":{"Domain":{"$type":"System.String","$value":"CONTOSO"},"Account":{"$type":"System.String","$value":"team"}}}]}
        """;
        using var harness = new Harness(json);

        var matches = await harness.Client.SearchAsync(IdentitySearchFilter.General, "Contoso Team", CancellationToken.None);

        matches.Single().IsContainer.ShouldBeTrue();
    }

    [Fact]
    public async Task SearchAsync_NoHits_ReturnsEmpty()
    {
        using var harness = new Harness("""{"count":0,"value":[]}""");

        var matches = await harness.Client.SearchAsync(IdentitySearchFilter.General, "nobody", CancellationToken.None);

        matches.ShouldBeEmpty();
    }

    [Fact]
    public async Task SearchAsync_Unauthorized_NamesTheIdentityReadScope()
    {
        using var harness = new Harness(string.Empty, HttpStatusCode.Unauthorized);

        var ex = await Should.ThrowAsync<DeniedException>(
            () => harness.Client.SearchAsync(IdentitySearchFilter.General, "Jane", CancellationToken.None));

        ex.Message.ShouldEndWith(" Resolving a person needs the Identity: Read PAT scope.");
    }

    [Fact]
    public async Task SearchAsync_MissingDomain_HasNoUniqueName()
    {
        var json = """{"value":[{"id":"a1b2c3d4-0000-4000-8000-000000000003","providerDisplayName":"John Roe","properties":{"Account":{"$type":"System.String","$value":"jroe"}}}]}""";
        using var harness = new Harness(json);

        var matches = await harness.Client.SearchAsync(IdentitySearchFilter.General, "John Roe", CancellationToken.None);

        matches.Single().UniqueName.ShouldBeNull();
    }

    [Fact]
    public async Task SearchAsync_MissingProperties_HasNoUniqueName()
    {
        var json = """{"value":[{"id":"a1b2c3d4-0000-4000-8000-000000000003","providerDisplayName":"John Roe"}]}""";
        using var harness = new Harness(json);

        var matches = await harness.Client.SearchAsync(IdentitySearchFilter.General, "John Roe", CancellationToken.None);

        matches.ShouldBe([new IdentityMatch("a1b2c3d4-0000-4000-8000-000000000003", "John Roe", null, IsContainer: false)]);
    }

    [Fact]
    public async Task SearchAsync_MissingValue_ReturnsEmpty()
    {
        using var harness = new Harness("""{"count":0}""");

        var matches = await harness.Client.SearchAsync(IdentitySearchFilter.General, "nobody", CancellationToken.None);

        matches.ShouldBeEmpty();
    }

    [Fact]
    public async Task SearchAsync_NonObjectElement_IsSkipped()
    {
        var json = """{"value":["x",null,{"id":"a1b2c3d4-0000-4000-8000-000000000001","providerDisplayName":"Jane Doe"}]}""";
        using var harness = new Harness(json);

        var matches = await harness.Client.SearchAsync(IdentitySearchFilter.General, "Jane", CancellationToken.None);

        matches.Single().DisplayName.ShouldBe("Jane Doe");
    }

    [Theory]
    [InlineData("""{"value":[{"providerDisplayName":"Jane Doe"}]}""")]
    [InlineData("""{"value":[{"id":null,"providerDisplayName":"Jane Doe"}]}""")]
    public async Task SearchAsync_MissingId_Throws(string json)
    {
        using var harness = new Harness(json);

        var ex = await Should.ThrowAsync<DefectException>(
            () => harness.Client.SearchAsync(IdentitySearchFilter.General, "Jane", CancellationToken.None));

        ex.Message.ShouldBe("Identity ID not found in identities response.");
    }

    [Fact]
    public async Task SearchAsync_MissingDisplayName_Throws()
    {
        using var harness = new Harness("""{"value":[{"id":"a1b2c3d4-0000-4000-8000-000000000001"}]}""");

        var ex = await Should.ThrowAsync<DefectException>(
            () => harness.Client.SearchAsync(IdentitySearchFilter.General, "Jane", CancellationToken.None));

        ex.Message.ShouldBe("Identity display name not found in identities response.");
    }
    private sealed class Harness : IDisposable
    {
        private readonly HttpClient _http;

        public Harness(string body, HttpStatusCode status = HttpStatusCode.OK)
        {
            Handler = new FakeHttpHandler(body, status);
            _http = TestHttp.Client(Handler);
            Client = new AzureDevOpsIdentityClient(_http, TestConstants.ServerUrl, TestConstants.Collection);
        }

        public FakeHttpHandler Handler { get; }

        public AzureDevOpsIdentityClient Client { get; }

        public void Dispose()
        {
            _http.Dispose();
            Handler.Dispose();
        }
    }
}
