using System.CommandLine;
using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Quill.AzureDevOps;
using Quill.Core;

namespace Quill.Cli.Commands.Pipeline;

internal static class RetryCommand
{
    public static Command Create(IServiceProvider serviceProvider)
    {
        var runIdArg = new Argument<int>("runId") { Description = "The pipeline run ID to retry" };

        runIdArg.Validators.Add(result =>
        {
            if (result.Tokens.Count > 0 && int.TryParse(result.Tokens[0].Value, CultureInfo.InvariantCulture, out var runId) && runId < 1)
            {
                result.AddError("runId must be a positive integer.");
            }
        });

        var command = new Command("retry", "Rerun the failed jobs of a pipeline run and print it to stdout as JSON")
        {
            runIdArg,
        };

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var runId = parseResult.GetValue(runIdArg);
            await ExecuteAsync(serviceProvider, runId, cancellationToken);
        });

        return command;
    }

    private static async Task ExecuteAsync(IServiceProvider serviceProvider, int runId, CancellationToken cancellationToken)
    {
        try
        {
            var buildClient = serviceProvider.GetRequiredService<AzureDevOpsBuildClient>();

            var run = await buildClient.RetryRunAsync(runId, cancellationToken);

            var result = PipelineRunResultBuilder.Build(run, null);

            Console.WriteLine(JsonSerializer.Serialize(result, CommandHelpers.Context.PipelineRunResult));
        }
        catch (Exception ex) when (ex is FileNotFoundException or InvalidOperationException or HttpRequestException)
        {
            CommandHelpers.HandleError(ex);
        }
    }
}
