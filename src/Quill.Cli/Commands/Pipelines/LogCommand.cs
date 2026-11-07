using System.CommandLine;
using System.CommandLine.Parsing;
using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Quill.Core;
using Quill.Core.Actions.Pipelines;

namespace Quill.Cli.Commands.Pipelines;

internal static class LogCommand
{
    public static Command Create(IServiceProvider serviceProvider)
    {
        var runIdArg = new Argument<int>("runId") { Description = "The pipeline run ID" };

        Validators.PositiveInteger(runIdArg, "runId");

        var logIdArg = new Argument<int>("logId") { Description = "The log ID, from pipeline view --with-stages" };

        Validators.PositiveInteger(logIdArg, "logId");

        var startLineOption = new Option<int?>("--start-line")
        {
            Description = "First line to print. Lines count from 1.",
        };

        Validators.PositiveInteger(startLineOption, "--start-line");

        var endLineOption = new Option<int?>("--end-line")
        {
            Description = "Last line to print, inclusive. Without it, prints to the end of the log.",
        };

        var tailOption = new Option<int?>("--tail")
        {
            Description = "Print the last n lines. Cannot be combined with --start-line or --end-line.",
        };

        Validators.PositiveInteger(tailOption, "--tail");

        var command = new Command("log", "Print a pipeline log, or a line range of it, to stdout as JSON")
        {
            runIdArg,
            logIdArg,
            startLineOption,
            endLineOption,
            tailOption,
        };

        command.Validators.Add(commandResult =>
        {
            if (!TryReadInt(commandResult, startLineOption, out var startLine)
                || !TryReadInt(commandResult, endLineOption, out var endLine)
                || !TryReadInt(commandResult, tailOption, out var tail))
            {
                return;
            }

            if (endLine is not null && endLine < (startLine ?? 1))
            {
                commandResult.AddError("--end-line must not be below --start-line.");
            }

            if (tail is not null && (startLine is not null || endLine is not null))
            {
                commandResult.AddError("--tail cannot be combined with --start-line or --end-line.");
            }
        });

        command.SetAction((parseResult, cancellationToken) =>
        {
            var request = new PipelineLogRequest(
                parseResult.GetValue(runIdArg),
                parseResult.GetValue(logIdArg),
                parseResult.GetValue(startLineOption),
                parseResult.GetValue(endLineOption),
                parseResult.GetValue(tailOption));

            return CommandHelpers.RunAsync(
                ct => LogAction.RunAsync(serviceProvider.GetRequiredService<IAzureDevOpsBuildClient>(), request, ct),
                CommandHelpers.Context.PipelineLogResult,
                cancellationToken);
        });

        return command;
    }

    private static bool TryReadInt(CommandResult commandResult, Option<int?> option, out int? value)
    {
        value = null;
        var tokens = commandResult.GetResult(option)?.Tokens;
        if (tokens is null || tokens.Count == 0)
        {
            return true;
        }

        if (!int.TryParse(tokens[0].Value, CultureInfo.InvariantCulture, out var parsed))
        {
            return false;
        }

        value = parsed;
        return true;
    }
}
