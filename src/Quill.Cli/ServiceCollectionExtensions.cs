using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quill.AzureDevOps;
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

        services.AddHttpClient<AzureDevOpsClient>()
            .ConfigureQuillHttpClient()
            .AddTypedClient((httpClient, serviceProvider) =>
            {
                var config = serviceProvider.GetRequiredService<QuillConfig>();
                return new AzureDevOpsClient(httpClient, config.ServerUrl, config.Collection, config.Project);
            });

        services.AddHttpClient<AzureDevOpsPullRequestClient>()
            .ConfigureQuillHttpClient()
            .AddTypedClient((httpClient, serviceProvider) =>
            {
                var config = serviceProvider.GetRequiredService<QuillConfig>();
                return new AzureDevOpsPullRequestClient(httpClient, config.ServerUrl, config.Collection, config.Project);
            });

        services.AddHttpClient<AzureDevOpsIdentityClient>()
            .ConfigureQuillHttpClient()
            .AddTypedClient((httpClient, serviceProvider) =>
            {
                var config = serviceProvider.GetRequiredService<QuillConfig>();
                return new AzureDevOpsIdentityClient(httpClient, config.ServerUrl, config.Collection);
            });

        return services;
    }

    private static IHttpClientBuilder ConfigureQuillHttpClient(this IHttpClientBuilder builder)
    {
        return builder.ConfigureHttpClient((serviceProvider, httpClient) =>
        {
            var pat = serviceProvider.GetRequiredService<IPatProvider>().GetPat();

            httpClient.Timeout = TimeSpan.FromSeconds(30);

            var patBytes = Convert.ToBase64String(Encoding.ASCII.GetBytes($":{pat}"));
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", patBytes);
        });
    }
}
