using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Quill.AzureDevOps;
using Quill.Core.Models;
using Shouldly;

namespace Quill.Cli.Tests;

public class ServiceWiringTests
{
    private const string ServerUrl = "https://devops.example.com";
    private const string Collection = "COL";
    private const string Project = "PROJ";

    [Fact]
    public void AddQuillServices_ResolvesAllThreeTypedClients()
    {
        using var provider = BuildProvider(handler: null);

        provider.GetRequiredService<AzureDevOpsClient>().ShouldNotBeNull();
        provider.GetRequiredService<AzureDevOpsPullRequestClient>().ShouldNotBeNull();
        provider.GetRequiredService<AzureDevOpsIdentityClient>().ShouldNotBeNull();
    }

    [Fact]
    public async Task AzureDevOpsClient_MapsConfigToConstructorArgsInOrder()
    {
        using var handler = new CapturingHandler();
        using var provider = BuildProvider(handler);

        var client = provider.GetRequiredService<AzureDevOpsClient>();
        await Should.ThrowAsync<HttpRequestException>(() => client.GetWorkItemAsync(1));

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
        await Should.ThrowAsync<HttpRequestException>(() => client.GetByIdAsync(1));

        handler.LastRequestUri.ShouldNotBeNull();
        handler.LastRequestUri.AbsoluteUri.ShouldContain($"/{Collection}/{Project}/_apis/git/pullrequests/1");
    }

    private static ServiceProvider BuildProvider(CapturingHandler? handler)
    {
        var services = new ServiceCollection();
        services.AddQuillServices();

        // Swap the real seams for deterministic stubs (registered last, so they win on resolve).
        services.AddSingleton<IQuillConfigProvider>(new StubConfigProvider(new QuillConfig
        {
            ServerUrl = ServerUrl,
            Collection = Collection,
            Project = Project,
            AllowedStates = ["Done"],
            AllowedParentStates = ["Done"],
        }));
        services.AddSingleton<IPatProvider>(new StubPatProvider());

        if (handler is not null)
        {
            // Override the primary handler on the named clients (the typed clients' default names).
            // Re-using the generic AddHttpClient<T>() overload would re-register the default typed-client
            // activator and clobber the AddTypedClient factory under test, so configure by name instead.
            services.AddHttpClient(nameof(AzureDevOpsClient)).ConfigurePrimaryHttpMessageHandler(() => handler);
            services.AddHttpClient(nameof(AzureDevOpsPullRequestClient)).ConfigurePrimaryHttpMessageHandler(() => handler);
            services.AddHttpClient(nameof(AzureDevOpsIdentityClient)).ConfigurePrimaryHttpMessageHandler(() => handler);
        }

        return services.BuildServiceProvider();
    }

    private sealed class StubConfigProvider(QuillConfig config) : IQuillConfigProvider
    {
        public QuillConfig Load() => config;
    }

    private sealed class StubPatProvider : IPatProvider
    {
        public string GetPat() => "test-pat";
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
