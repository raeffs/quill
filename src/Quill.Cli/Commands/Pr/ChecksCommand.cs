using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Quill.Core;
using Quill.Core.Actions.Pr;

namespace Quill.Cli.Commands.Pr;

internal static class ChecksCommand
{
    public static Command Create(IServiceProvider serviceProvider)
    {
        var idArg = new Argument<int>("id") { Description = "The Azure DevOps pull request ID" };

        Validators.PositiveInteger(idArg, "id");

        var command = new Command("checks", "Print the policy evaluations that gate a pull request as JSON")
        {
            idArg,
        };

        command.SetAction((parseResult, cancellationToken) =>
        {
            var id = parseResult.GetValue(idArg);

            return CommandHelpers.RunAsync(
                ct => ChecksAction.RunAsync(serviceProvider.GetRequiredService<IAzureDevOpsPullRequestClient>(), id, ct),
                CommandHelpers.Context.PullRequestChecksResult,
                cancellationToken);
        });

        return command;
    }
}
