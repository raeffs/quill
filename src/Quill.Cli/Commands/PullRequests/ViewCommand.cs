using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quill.Core;
using Quill.Core.Actions.PullRequests;
using Quill.Core.Models;

namespace Quill.Cli.Commands.PullRequests;

internal static class ViewCommand
{
    public static Command Create(IServiceProvider serviceProvider)
    {
        var idArg = new Argument<int>("id") { Description = "The Azure DevOps pull request ID to view" };

        var withThreadsOption = new Option<bool>("--with-threads")
        {
            Description = "Append a `threads` array with the PR's review threads (same payload as `pr threads`).",
        };

        var command = new Command("view", "Print a pull request to stdout as JSON")
        {
            idArg,
            withThreadsOption,
        };

        command.SetAction((parseResult, cancellationToken) =>
        {
            var id = parseResult.GetValue(idArg);
            var withThreads = parseResult.GetValue(withThreadsOption);

            return CommandHelpers.RunAsync(
                ct => ViewAction.RunAsync(
                    serviceProvider.GetRequiredService<IAzureDevOpsPullRequestClient>(),
                    serviceProvider.GetRequiredService<IAzureDevOpsClient>(),
                    serviceProvider.GetRequiredService<IWorkItemBatchFetcher>(),
                    serviceProvider.GetRequiredService<IIdentityClient>(),
                    serviceProvider.GetRequiredService<QuillConfig>(),
                    serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Quill.PrView"),
                    id,
                    withThreads,
                    ct),
                CommandHelpers.Context.PullRequestViewResult,
                cancellationToken);
        });

        return command;
    }
}
