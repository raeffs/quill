using System.CommandLine;
using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Quill.AzureDevOps;
using Quill.Core;

namespace Quill.Cli.Commands.Pipeline;

internal static class WaitCommand
{
    public static Command Create(IServiceProvider serviceProvider)
    {
        var runIdArg = new Argument<int>("runId") { Description = "The pipeline run ID to wait for" };

        runIdArg.Validators.Add(result =>
        {
            if (result.Tokens.Count > 0 && int.TryParse(result.Tokens[0].Value, CultureInfo.InvariantCulture, out var runId) && runId < 1)
            {
                result.AddError("runId must be a positive integer.");
            }
        });

        var timeoutOption = new Option<int>("--timeout")
        {
            Description = "Seconds to wait before printing the run as it is. Defaults to 600.",
            DefaultValueFactory = _ => 600,
        };

        timeoutOption.Validators.Add(result =>
        {
            if (result.Tokens.Count > 0 && int.TryParse(result.Tokens[0].Value, CultureInfo.InvariantCulture, out var timeout) && timeout < 1)
            {
                result.AddError("--timeout must be at least 1.");
            }
        });

        var command = new Command("wait", "Wait until a pipeline run completes and print it to stdout as JSON")
        {
            runIdArg,
            timeoutOption,
        };

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var runId = parseResult.GetValue(runIdArg);
            var timeout = parseResult.GetValue(timeoutOption);
            await ExecuteAsync(serviceProvider, runId, timeout, cancellationToken);
        });

        return command;
    }

    private static async Task ExecuteAsync(
        IServiceProvider serviceProvider,
        int runId,
        int timeout,
        CancellationToken cancellationToken)
    {
        try
        {
            var buildClient = serviceProvider.GetRequiredService<AzureDevOpsBuildClient>();

            var waiter = new PipelineRunWaiter(buildClient, SystemWaitClock.Instance);
            var run = await waiter.WaitAsync(runId, TimeSpan.FromSeconds(timeout), cancellationToken);

            var result = PipelineRunResultBuilder.Build(run, null);

            Console.WriteLine(JsonSerializer.Serialize(result, CommandHelpers.Context.PipelineRunResult));
        }
        catch (Exception ex) when (ex is FileNotFoundException or InvalidOperationException or HttpRequestException)
        {
            CommandHelpers.HandleError(ex);
        }
    }
}
