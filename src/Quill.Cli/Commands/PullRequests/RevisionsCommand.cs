using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Quill.Core;
using Quill.Core.Actions.PullRequests;

namespace Quill.Cli.Commands.PullRequests;

internal static class RevisionsCommand
{
    public static Command Create(IServiceProvider serviceProvider)
    {
        var idArg = new Argument<int>("id") { Description = "The Azure DevOps pull request ID whose revisions to read" };

        Validators.PositiveInteger(idArg, "id");

        var command = new Command("revisions", "Print a pull request's revisions as JSON, newest first")
        {
            idArg,
        };

        command.SetAction((parseResult, cancellationToken) =>
        {
            var id = parseResult.GetValue(idArg);

            return CommandHelpers.RunAsync(
                ct => RevisionsAction.RunAsync(serviceProvider.GetRequiredService<IAzureDevOpsPullRequestClient>(), id, ct),
                CommandHelpers.Context.ListPullRequestRevisionResult,
                cancellationToken);
        });

        return command;
    }
}
