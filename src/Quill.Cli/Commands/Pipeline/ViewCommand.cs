using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Quill.Core;
using Quill.Core.Actions;

namespace Quill.Cli.Commands.Pipeline;

internal static class ViewCommand
{
    public static Command Create(IServiceProvider serviceProvider)
    {
        var runIdArg = new Argument<int>("runId") { Description = "The pipeline run ID to view" };

        Validators.PositiveInteger(runIdArg, "runId");

        var withStagesOption = new Option<bool>("--with-stages")
        {
            Description = "Append a `stages` array with the run's stages, jobs, tasks and the issues each task reported.",
        };

        var command = new Command("view", "Print a pipeline run to stdout as JSON")
        {
            runIdArg,
            withStagesOption,
        };

        command.SetAction((parseResult, cancellationToken) =>
        {
            var runId = parseResult.GetValue(runIdArg);
            var withStages = parseResult.GetValue(withStagesOption);

            return CommandHelpers.RunAsync(
                ct => PipelineViewAction.RunAsync(serviceProvider.GetRequiredService<IAzureDevOpsBuildClient>(), runId, withStages, ct),
                CommandHelpers.Context.PipelineRunResult,
                cancellationToken);
        });

        return command;
    }
}
