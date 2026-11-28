using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Quill.AzureDevOps;
using Quill.Core;
using Quill.Core.Markdown;
using Quill.Core.Models;
using Quill.Tests.Shared.Fakes;
using Shouldly;

namespace Quill.Cli.Tests;

public class ServiceWiringTests
{
    private const string ServerUrl = "https://devops.example.com";
    private const string Collection = "COL";
    private const string Project = "PROJ";

    [Fact]
    public void AddQuillServices_ResolvesEveryTypedClient()
    {
        using var provider = BuildProvider(handler: null);

        provider.GetRequiredService<AzureDevOpsClient>().ShouldNotBeNull();
        provider.GetRequiredService<AzureDevOpsPullRequestClient>().ShouldNotBeNull();
        provider.GetRequiredService<AzureDevOpsIdentityClient>().ShouldNotBeNull();
        provider.GetRequiredService<AzureDevOpsBuildClient>().ShouldNotBeNull();
    }

    [Fact]
    public void AddQuillServices_ResolvesOnePersonResolverForTheRun()
    {
        using var provider = BuildProvider(handler: null);

        var first = provider.GetRequiredService<PersonResolver>();

        provider.GetRequiredService<PersonResolver>().ShouldBeSameAs(first);
    }

    [Fact]
    public void AddQuillServices_ResolvesEveryClientInterface()
    {
        using var provider = BuildProvider(handler: null);

        provider.GetRequiredService<IAzureDevOpsClient>().ShouldBeOfType<AzureDevOpsClient>();
        provider.GetRequiredService<IWorkItemBatchFetcher>().ShouldBeOfType<AzureDevOpsClient>();
        provider.GetRequiredService<IAzureDevOpsPullRequestClient>().ShouldBeOfType<AzureDevOpsPullRequestClient>();
        provider.GetRequiredService<IIdentityClient>().ShouldBeOfType<AzureDevOpsIdentityClient>();
        provider.GetRequiredService<IAzureDevOpsBuildClient>().ShouldBeOfType<AzureDevOpsBuildClient>();
    }

    [Fact]
    public void AddQuillServices_ResolvesRichTextOnce()
    {
        using var handler = new CapturingHandler();
        using var provider = BuildProvider(handler);

        var first = provider.GetRequiredService<RichText>();
        var second = provider.GetRequiredService<RichText>();

        second.ShouldBeSameAs(first);
    }

    [Fact]
    public async Task AzureDevOpsClient_MapsConfigToConstructorArgsInOrder()
    {
        using var handler = new CapturingHandler();
        using var provider = BuildProvider(handler);

        var client = provider.GetRequiredService<AzureDevOpsClient>();
        await Should.ThrowAsync<UnavailableException>(() => client.GetWorkItemAsync(1));

        // Collection must precede Project; a transposed mapping would emit /PROJ/COL/.
        handler.LastRequestUri.ShouldNotBeNull();
        handler.LastRequestUri.AbsoluteUri.ShouldContain($"/{Collection}/{Project}/_apis/wit/workitems/1");
    }

    [Fact]
    public async Task PullRequestClient_MapsConfigToConstructorArgsInOrder()
    {
        using var handler = new CapturingHandler();
        using var provider = BuildProvider(handler);

        var client = provider.GetRequiredService<AzureDevOpsPullRequestClient>();
        await Should.ThrowAsync<UnavailableException>(() => client.GetByIdAsync(1));

        handler.LastRequestUri.ShouldNotBeNull();
        handler.LastRequestUri.AbsoluteUri.ShouldContain($"/{Collection}/{Project}/_apis/git/pullrequests/1");
    }

    [Fact]
    public async Task BuildClient_MapsConfigToConstructorArgsInOrder()
    {
        using var handler = new CapturingHandler();
        using var provider = BuildProvider(handler);

        var client = provider.GetRequiredService<AzureDevOpsBuildClient>();
        await Should.ThrowAsync<UnavailableException>(() => client.GetRunAsync(1));

        handler.LastRequestUri.ShouldNotBeNull();
        handler.LastRequestUri.AbsoluteUri.ShouldContain($"/{Collection}/{Project}/_apis/build/builds/1");
    }

    [Fact]
    public async Task AddQuillServices_ClassifiesTheFailuresOfEveryTypedClient()
    {
        using var handler = new CapturingHandler();
        using var provider = BuildProvider(handler);
        const string expected = "The server answered 500 (Internal Server Error).";

        (await Should.ThrowAsync<UnavailableException>(() => provider.GetRequiredService<AzureDevOpsClient>().GetWorkItemAsync(1))).Message.ShouldBe(expected);
        (await Should.ThrowAsync<UnavailableException>(() => provider.GetRequiredService<AzureDevOpsPullRequestClient>().GetByIdAsync(1))).Message.ShouldBe(expected);
        (await Should.ThrowAsync<UnavailableException>(() => provider.GetRequiredService<AzureDevOpsBuildClient>().GetRunAsync(1))).Message.ShouldBe(expected);
        (await Should.ThrowAsync<UnavailableException>(() => provider.GetRequiredService<AzureDevOpsIdentityClient>().GetCurrentUserAsync())).Message.ShouldBe(expected);
    }

    [Fact]
    public void AddQuillServices_LeavesTheTimeoutToTheHandler()
    {
        using var provider = BuildProvider(handler: null);

        using var httpClient = provider.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(AzureDevOpsServer));

        httpClient.Timeout.ShouldBe(Timeout.InfiniteTimeSpan);
    }

    [Fact]
    public void AddQuillServices_RegistersTheFileStore()
    {
        var services = new ServiceCollection();
        services.AddQuillServices();
        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<IPersonStore>().ShouldBeOfType<FilePersonStore>();
    }

    [Fact]
    public async Task PersonResolver_SecondRunWithTheSamePat_SendsNoConnectionDataRequest()
    {
        var store = new InMemoryPersonStore();
        using var firstHandler = new ConnectionDataHandler();
        using var secondHandler = new ConnectionDataHandler();
        using var first = BuildProvider(firstHandler, store);
        using var second = BuildProvider(secondHandler, store);

        var firstUser = await first.GetRequiredService<PersonResolver>().MeAsync();
        var secondUser = await second.GetRequiredService<PersonResolver>().MeAsync();

        firstHandler.Requests.Count.ShouldBe(1);
        secondHandler.Requests.Count.ShouldBe(0);
        secondUser.ShouldBe(firstUser);
    }

    [Fact]
    public async Task PersonResolver_SecondRunWithAnotherPat_AsksTheServer()
    {
        var store = new InMemoryPersonStore();
        using var firstHandler = new ConnectionDataHandler();
        using var secondHandler = new ConnectionDataHandler();
        using var first = BuildProvider(firstHandler, store);
        using var second = BuildProvider(secondHandler, store, pat: "other-pat");

        await first.GetRequiredService<PersonResolver>().MeAsync();
        await second.GetRequiredService<PersonResolver>().MeAsync();

        secondHandler.Requests.Count.ShouldBe(1);
    }

    [Fact]
    public async Task PersonResolver_SecondRunWithAnotherCollection_AsksTheServer()
    {
        var store = new InMemoryPersonStore();
        using var firstHandler = new ConnectionDataHandler();
        using var secondHandler = new ConnectionDataHandler();
        using var first = BuildProvider(firstHandler, store);
        using var second = BuildProvider(secondHandler, store, collection: "OTHER");

        await first.GetRequiredService<PersonResolver>().MeAsync();
        await second.GetRequiredService<PersonResolver>().MeAsync();

        secondHandler.Requests.Count.ShouldBe(1);
    }

    [Fact]
    public void PersonResolver_NoPat_FailsLikeTheIdentityClient()
    {
        using var provider = BuildProvider(
            handler: null,
            patProvider: new ThrowingPatProvider(),
            configProvider: new ThrowingConfigProvider());

        var expected = Should.Throw<UsageException>(() => provider.GetRequiredService<AzureDevOpsIdentityClient>());
        var actual = Should.Throw<UsageException>(() => provider.GetRequiredService<PersonResolver>());

        actual.Message.ShouldBe(expected.Message);
    }

    private static ServiceProvider BuildProvider(
        HttpMessageHandler? handler,
        IPersonStore? store = null,
        string pat = "test-pat",
        string collection = Collection,
        IPatProvider? patProvider = null,
        IQuillConfigProvider? configProvider = null)
    {
        var services = new ServiceCollection();
        services.AddQuillServices();

        // Swap the real seams for deterministic stubs (registered last, so they win on resolve).
        services.AddSingleton(store ?? new InMemoryPersonStore());
        services.AddSingleton(configProvider ?? new StubConfigProvider(new QuillConfig
        {
            ServerUrl = ServerUrl,
            Collection = collection,
            Project = Project,
            AllowedStates = ["Done"],
        }));
        services.AddSingleton(patProvider ?? new StubPatProvider(pat));

        if (handler is not null)
        {
            // Override the primary handler on the named client (the typed client's default name).
            // Re-using the generic AddHttpClient<T>() overload would re-register the default typed-client
            // activator and clobber the AddTypedClient factory under test, so configure by name instead.
            services.AddHttpClient(nameof(AzureDevOpsServer)).ConfigurePrimaryHttpMessageHandler(() => handler);
        }

        return services.BuildServiceProvider();
    }

    private sealed class StubConfigProvider(QuillConfig config) : IQuillConfigProvider
    {
        public QuillConfig Load() => config;
    }

    private sealed class StubPatProvider(string pat) : IPatProvider
    {
        public string GetPat() => pat;
    }

    private sealed class ThrowingPatProvider : IPatProvider
    {
        public string GetPat() => throw new UsageException("QUILL_PAT environment variable is not set.");
    }

    private sealed class ThrowingConfigProvider : IQuillConfigProvider
    {
        public QuillConfig Load() => throw new UsageException("no config");
    }

    private sealed class ConnectionDataHandler : HttpMessageHandler
    {
#pragma warning disable CA1002, MA0016 // The tests count the recorded requests
        public List<Uri> Requests { get; } = [];
#pragma warning restore CA1002, MA0016

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request.RequestUri!);
            var response = request.RequestUri!.AbsolutePath.Contains("_apis/connectionData", StringComparison.Ordinal)
                ? new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("""{"authenticatedUser":{"id":"user-1","providerDisplayName":"Jane Doe"}}"""),
                }
                : new HttpResponseMessage(HttpStatusCode.NotFound);
            return Task.FromResult(response);
        }
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        public Uri? LastRequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequestUri = request.RequestUri;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));
        }
    }
}
