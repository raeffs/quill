using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Quill.Core;
using Quill.Core.Actions.Pipelines;

namespace Quill.Cli.Commands.Pipeline;

internal static class RetryCommand
{
    public static Command Create(IServiceProvider serviceProvider)
    {
        var runIdArg = new Argument<int>("runId") { Description = "The pipeline run ID to retry" };

        Validators.PositiveInteger(runIdArg, "runId");

        var command = new Command("retry", "Rerun the failed jobs of a pipeline run and print it to stdout as JSON")
        {
            runIdArg,
        };

        command.SetAction((parseResult, cancellationToken) =>
        {
            var runId = parseResult.GetValue(runIdArg);
            return CommandHelpers.RunAsync(
                ct => RetryAction.RunAsync(serviceProvider.GetRequiredService<IAzureDevOpsBuildClient>(), runId, ct),
                CommandHelpers.Context.PipelineRunResult,
                cancellationToken);
        });

        return command;
    }
}
