using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quill.Core;
using Quill.Core.Actions.Pr;
using Quill.Core.Models;

namespace Quill.Cli.Commands.Pr;

internal static class ThreadsCommand
{
    public static Command Create(IServiceProvider serviceProvider)
    {
        var idArg = new Argument<int>("id") { Description = "The Azure DevOps pull request ID whose threads to read" };

        var statusOption = new Option<string[]>("--status")
        {
            Description = "Filter by thread status (repeat to OR). Accepts active, fixed, wontFix, closed, pending, byDesign. Omit to return all.",
            AllowMultipleArgumentsPerToken = false,
        };

        var limitOption = new Option<int?>("--limit")
        {
            Description = "Return only the N most recent threads (must be >= 1). Omit to return all threads.",
        };

        Validators.PositiveInteger(limitOption, "--limit");

        var command = new Command("threads", "Print a pull request's review threads as JSON, newest first")
        {
            idArg,
            statusOption,
            limitOption,
        };

        command.SetAction((parseResult, cancellationToken) =>
        {
            var request = new PrThreadsRequest(
                parseResult.GetValue(idArg),
                parseResult.GetValue(statusOption) ?? [],
                parseResult.GetValue(limitOption));

            return CommandHelpers.RunAsync(
                ct => ThreadsAction.RunAsync(
                    serviceProvider.GetRequiredService<IAzureDevOpsPullRequestClient>(),
                    serviceProvider.GetRequiredService<IAzureDevOpsClient>(),
                    serviceProvider.GetRequiredService<QuillConfig>(),
                    serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Quill.PrThreads"),
                    request,
                    ct),
                CommandHelpers.Context.ListPullRequestThreadResult,
                cancellationToken);
        });

        return command;
    }
}
