using System.CommandLine;
using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Quill.AzureDevOps;
using Quill.Core;
using Quill.Core.Models;

namespace Quill.Cli.Commands.Pipeline;

internal static class RunsCommand
{
    public static Command Create(IServiceProvider serviceProvider)
    {
        var pipelineOption = new Option<int?>("--pipeline")
        {
            Description = "Pipeline ID, from pipeline list.",
        };

        pipelineOption.Validators.Add(result =>
        {
            if (result.Tokens.Count > 0 && int.TryParse(result.Tokens[0].Value, CultureInfo.InvariantCulture, out var pipeline) && pipeline < 1)
            {
                result.AddError("--pipeline must be a positive integer.");
            }
        });

        var branchOption = new Option<string?>("--branch")
        {
            Description = "Branch short name, for example main. Without it, runs from every branch.",
        };

        var statusOption = new Option<string?>("--status")
        {
            Description = "Run status, for example inProgress or completed. Passed to the server as is.",
        };

        var resultOption = new Option<string?>("--result")
        {
            Description = "Run result, for example failed or succeeded. Passed to the server as is.",
        };

        var reasonOption = new Option<string?>("--reason")
        {
            Description = "Run reason, for example manual or pullRequest. Passed to the server as is.",
        };

        var limitOption = new Option<int>("--limit")
        {
            Description = "Maximum number of results. Default: 50.",
            DefaultValueFactory = _ => 50,
        };

        limitOption.Validators.Add(result =>
        {
            if (result.Tokens.Count > 0 && int.TryParse(result.Tokens[0].Value, CultureInfo.InvariantCulture, out var limit) && limit < 1)
            {
                result.AddError("--limit must be a positive integer.");
            }
        });

        var command = new Command("runs", "List pipeline runs, newest first, and print them as a JSON array")
        {
            pipelineOption,
            branchOption,
            statusOption,
            resultOption,
            reasonOption,
            limitOption,
        };

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var query = new PipelineRunListQuery
            {
                PipelineId = parseResult.GetValue(pipelineOption),
                Branch = parseResult.GetValue(branchOption),
                Status = parseResult.GetValue(statusOption),
                Result = parseResult.GetValue(resultOption),
                Reason = parseResult.GetValue(reasonOption),
                Top = parseResult.GetValue(limitOption),
            };
            await ExecuteAsync(serviceProvider, query, cancellationToken);
        });

        return command;
    }

    private static async Task ExecuteAsync(
        IServiceProvider serviceProvider,
        PipelineRunListQuery query,
        CancellationToken cancellationToken)
    {
        try
        {
            var buildClient = serviceProvider.GetRequiredService<AzureDevOpsBuildClient>();

            var runs = await buildClient.ListRunsAsync(query, cancellationToken);
            var results = runs.Select(PipelineRunResultBuilder.Build).ToList();

            Console.WriteLine(JsonSerializer.Serialize(results, CommandHelpers.Context.ListPipelineRunResult));
        }
        catch (Exception ex) when (ex is FileNotFoundException or InvalidOperationException or HttpRequestException)
        {
            CommandHelpers.HandleError(ex);
        }
    }
}
