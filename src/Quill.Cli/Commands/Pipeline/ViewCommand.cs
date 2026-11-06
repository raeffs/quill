using System.CommandLine;
using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Quill.AzureDevOps;
using Quill.Core;
using Quill.Core.Models;

namespace Quill.Cli.Commands.Pipeline;

internal static class ViewCommand
{
    public static Command Create(IServiceProvider serviceProvider)
    {
        var runIdArg = new Argument<int>("runId") { Description = "The pipeline run ID to view" };

        runIdArg.Validators.Add(result =>
        {
            if (result.Tokens.Count > 0 && int.TryParse(result.Tokens[0].Value, CultureInfo.InvariantCulture, out var runId) && runId < 1)
            {
                result.AddError("runId must be a positive integer.");
            }
        });

        var withStagesOption = new Option<bool>("--with-stages")
        {
            Description = "Append a `stages` array with the run's stages, jobs, tasks and the issues each task reported.",
        };

        var command = new Command("view", "Print a pipeline run to stdout as JSON")
        {
            runIdArg,
            withStagesOption,
        };

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var runId = parseResult.GetValue(runIdArg);
            var withStages = parseResult.GetValue(withStagesOption);
            await ExecuteAsync(serviceProvider, runId, withStages, cancellationToken);
        });

        return command;
    }

    private static async Task ExecuteAsync(
        IServiceProvider serviceProvider,
        int runId,
        bool withStages,
        CancellationToken cancellationToken)
    {
        try
        {
            var buildClient = serviceProvider.GetRequiredService<AzureDevOpsBuildClient>();

            var run = await buildClient.GetRunAsync(runId, cancellationToken);
            IReadOnlyList<PipelineStageResult>? stages = null;
            if (withStages)
            {
                var records = await buildClient.GetTimelineAsync(runId, cancellationToken);
                stages = PipelineStageTreeBuilder.Build(records);
            }

            var result = PipelineRunResultBuilder.Build(run, stages);

            Console.WriteLine(JsonSerializer.Serialize(result, CommandHelpers.Context.PipelineRunResult));
        }
        catch (Exception ex) when (ex is FileNotFoundException or InvalidOperationException or HttpRequestException)
        {
            CommandHelpers.HandleError(ex);
        }
    }
}
