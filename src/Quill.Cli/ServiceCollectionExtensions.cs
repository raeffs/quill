using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quill.AzureDevOps;
using Quill.Core;
using Quill.Core.Markdown;
using Quill.Core.Models;

namespace Quill.Cli;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddQuillServices(this IServiceCollection services)
    {
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Warning);
        });

        services.AddSingleton<IQuillConfigProvider, QuillConfigProvider>();
        services.AddSingleton<IPatProvider, EnvironmentPatProvider>();

        // Loaded lazily on first resolve, so `quill --help` and arg validation pay zero config cost.
        services.AddSingleton<QuillConfig>(sp => sp.GetRequiredService<IQuillConfigProvider>().Load());

        services.AddTransient(_ => new FailureModeHandler(TimeSpan.FromSeconds(30)));

        services.AddHttpClient<AzureDevOpsServer>()
            .ConfigureQuillHttpClient()
            .AddTypedClient((httpClient, serviceProvider) =>
            {
                var config = serviceProvider.GetRequiredService<QuillConfig>();
                return new AzureDevOpsServer(httpClient, config.ServerUrl, config.Collection, config.Project);
            });

        services.AddTransient(sp => new AzureDevOpsClient(sp.GetRequiredService<AzureDevOpsServer>()));
        services.AddTransient(sp => new AzureDevOpsIdentityClient(sp.GetRequiredService<AzureDevOpsServer>()));
        services.AddTransient(sp => new AzureDevOpsPullRequestClient(sp.GetRequiredService<AzureDevOpsServer>()));
        services.AddTransient(sp => new AzureDevOpsBuildClient(sp.GetRequiredService<AzureDevOpsServer>()));

        services.AddTransient<IAzureDevOpsClient>(sp => sp.GetRequiredService<AzureDevOpsClient>());
        services.AddTransient<IWorkItemBatchFetcher>(sp => sp.GetRequiredService<AzureDevOpsClient>());
        services.AddTransient<IAzureDevOpsPullRequestClient>(sp => sp.GetRequiredService<AzureDevOpsPullRequestClient>());
        services.AddTransient<IIdentityClient>(sp => sp.GetRequiredService<AzureDevOpsIdentityClient>());
        services.AddTransient<IAzureDevOpsBuildClient>(sp => sp.GetRequiredService<AzureDevOpsBuildClient>());

        services.AddSingleton<RichText>();
        services.AddSingleton<IPersonStore>(_ => new FilePersonStore(PersonCachePath.Default()));
        services.AddSingleton(sp =>
        {
            var identity = sp.GetRequiredService<IIdentityClient>();
            var config = sp.GetRequiredService<QuillConfig>();
            var pat = sp.GetRequiredService<IPatProvider>().GetPat();
            var cache = new PersonCache(
                sp.GetRequiredService<IPersonStore>(),
                PersonCacheScope.For(config.ServerUrl, config.Collection, pat),
                TimeProvider.System);
            return new IdentityResolver(identity, cache);
        });

        return services;
    }

    private static IHttpClientBuilder ConfigureQuillHttpClient(this IHttpClientBuilder builder)
    {
        return builder
            .ConfigureHttpClient((serviceProvider, httpClient) =>
            {
                var pat = serviceProvider.GetRequiredService<IPatProvider>().GetPat();

                httpClient.Timeout = Timeout.InfiniteTimeSpan;

                var patBytes = Convert.ToBase64String(Encoding.ASCII.GetBytes($":{pat}"));
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", patBytes);
            })
            .AddHttpMessageHandler<FailureModeHandler>();
    }
}
