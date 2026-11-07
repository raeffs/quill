using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Quill.Core;
using Quill.Core.Actions.Pipelines;
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

        Validators.PositiveInteger(pipelineOption, "--pipeline");

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

        Validators.PositiveInteger(limitOption, "--limit");

        var command = new Command("runs", "List pipeline runs, newest first, and print them as a JSON array")
        {
            pipelineOption,
            branchOption,
            statusOption,
            resultOption,
            reasonOption,
            limitOption,
        };

        command.SetAction((parseResult, cancellationToken) =>
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

            return CommandHelpers.RunAsync(
                ct => RunsAction.RunAsync(serviceProvider.GetRequiredService<IAzureDevOpsBuildClient>(), query, ct),
                CommandHelpers.Context.ListPipelineRunResult,
                cancellationToken);
        });

        return command;
    }
}
