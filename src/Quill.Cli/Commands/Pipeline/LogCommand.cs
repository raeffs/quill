using System.CommandLine;
using System.CommandLine.Parsing;
using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Quill.AzureDevOps;
using Quill.Core;

namespace Quill.Cli.Commands.Pipeline;

internal static class LogCommand
{
    public static Command Create(IServiceProvider serviceProvider)
    {
        var runIdArg = new Argument<int>("runId") { Description = "The pipeline run ID" };

        runIdArg.Validators.Add(result =>
        {
            if (result.Tokens.Count > 0 && int.TryParse(result.Tokens[0].Value, CultureInfo.InvariantCulture, out var runId) && runId < 1)
            {
                result.AddError("runId must be a positive integer.");
            }
        });

        var logIdArg = new Argument<int>("logId") { Description = "The log ID, from pipeline view --with-stages" };

        logIdArg.Validators.Add(result =>
        {
            if (result.Tokens.Count > 0 && int.TryParse(result.Tokens[0].Value, CultureInfo.InvariantCulture, out var logId) && logId < 1)
            {
                result.AddError("logId must be a positive integer.");
            }
        });

        var startLineOption = new Option<int?>("--start-line")
        {
            Description = "First line to print. Lines count from 1.",
        };

        startLineOption.Validators.Add(result =>
        {
            if (result.Tokens.Count > 0 && int.TryParse(result.Tokens[0].Value, CultureInfo.InvariantCulture, out var startLine) && startLine < 1)
            {
                result.AddError("--start-line must be a positive integer.");
            }
        });

        var endLineOption = new Option<int?>("--end-line")
        {
            Description = "Last line to print, inclusive. Without it, prints to the end of the log.",
        };

        var tailOption = new Option<int?>("--tail")
        {
            Description = "Print the last n lines. Cannot be combined with --start-line or --end-line.",
        };

        tailOption.Validators.Add(result =>
        {
            if (result.Tokens.Count > 0 && int.TryParse(result.Tokens[0].Value, CultureInfo.InvariantCulture, out var tail) && tail < 1)
            {
                result.AddError("--tail must be a positive integer.");
            }
        });

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

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var runId = parseResult.GetValue(runIdArg);
            var logId = parseResult.GetValue(logIdArg);
            var startLine = parseResult.GetValue(startLineOption);
            var endLine = parseResult.GetValue(endLineOption);
            var tail = parseResult.GetValue(tailOption);
            await ExecuteAsync(serviceProvider, runId, logId, startLine, endLine, tail, cancellationToken);
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

    private static async Task ExecuteAsync(
        IServiceProvider serviceProvider,
        int runId,
        int logId,
        int? startLine,
        int? endLine,
        int? tail,
        CancellationToken cancellationToken)
    {
        try
        {
            var buildClient = serviceProvider.GetRequiredService<AzureDevOpsBuildClient>();

            if (tail is not null)
            {
                var lineCount = await buildClient.GetLogLineCountAsync(runId, logId, cancellationToken);
                startLine = PipelineLogResultBuilder.TailStartLine(lineCount, tail.Value);
                endLine = null;
            }

            var lines = await buildClient.GetLogLinesAsync(runId, logId, startLine, endLine, cancellationToken);
            var result = PipelineLogResultBuilder.Build(runId, logId, startLine, lines);

            Console.WriteLine(JsonSerializer.Serialize(result, CommandHelpers.Context.PipelineLogResult));
        }
        catch (Exception ex) when (ex is FileNotFoundException or InvalidOperationException or HttpRequestException)
        {
            CommandHelpers.HandleError(ex);
        }
    }
}
