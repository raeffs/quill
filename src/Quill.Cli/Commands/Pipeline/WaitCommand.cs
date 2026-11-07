using System.CommandLine;
using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Quill.Core;
using Quill.Core.Actions;

namespace Quill.Cli.Commands.Pipeline;

internal static class WaitCommand
{
    public static Command Create(IServiceProvider serviceProvider)
    {
        var runIdArg = new Argument<int>("runId") { Description = "The pipeline run ID to wait for" };

        Validators.PositiveInteger(runIdArg, "runId");

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

        command.SetAction((parseResult, cancellationToken) =>
        {
            var runId = parseResult.GetValue(runIdArg);
            var timeout = parseResult.GetValue(timeoutOption);
            return CommandHelpers.RunAsync(
                ct => PipelineWaitAction.RunAsync(
                    serviceProvider.GetRequiredService<IAzureDevOpsBuildClient>(),
                    SystemWaitClock.Instance,
                    runId,
                    TimeSpan.FromSeconds(timeout),
                    ct),
                CommandHelpers.Context.PipelineRunResult,
                cancellationToken);
        });

        return command;
    }
}
