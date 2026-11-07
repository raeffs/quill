using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Quill.Core;
using Quill.Core.Actions;
using Quill.Core.Models;

namespace Quill.Cli.Commands.Pipeline;

internal static class TriggerCommand
{
    public static Command Create(IServiceProvider serviceProvider)
    {
        var pipelineIdArg = new Argument<int>("pipelineId") { Description = "The pipeline ID, from pipeline list" };

        Validators.PositiveInteger(pipelineIdArg, "pipelineId");

        var branchOption = new Option<string?>("--branch")
        {
            Description = "Branch short name, for example main. Without it, the server runs the pipeline's default branch.",
        };

        branchOption.Validators.Add(result =>
        {
            if (result.Tokens.Count > 0 && string.IsNullOrEmpty(result.Tokens[0].Value))
            {
                result.AddError("--branch must not be empty.");
            }
        });

        var parameterOption = new Option<string[]>("--parameter")
        {
            Description = "A template parameter as name=value. The value is sent as a string. Repeat for more than one.",
            AllowMultipleArgumentsPerToken = false,
        };

        parameterOption.Validators.Add(result =>
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var token in result.Tokens)
            {
                if (!PipelineParameters.TrySplit(token.Value, out var name, out _))
                {
                    result.AddError($"--parameter must be name=value: '{token.Value}'.");
                    return;
                }

                if (!names.Add(name))
                {
                    result.AddError($"--parameter '{name}' is given more than once.");
                    return;
                }
            }
        });

        var skipStageOption = new Option<string[]>("--skip-stage")
        {
            Description = "A YAML stage name to skip, as pipeline view prints it in a stage's name. Repeat for more than one.",
            AllowMultipleArgumentsPerToken = false,
        };

        skipStageOption.Validators.Add(result =>
        {
            if (result.Tokens.Any(token => string.IsNullOrEmpty(token.Value)))
            {
                result.AddError("--skip-stage must not be empty.");
            }
        });

        var command = new Command("trigger", "Queue a pipeline run and print it to stdout as JSON")
        {
            pipelineIdArg,
            branchOption,
            parameterOption,
            skipStageOption,
        };

        command.SetAction((parseResult, cancellationToken) =>
        {
            var parameters = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var token in parseResult.GetValue(parameterOption) ?? [])
            {
                PipelineParameters.TrySplit(token, out var name, out var value);
                parameters[name] = value;
            }

            var request = new PipelineTriggerRequest
            {
                PipelineId = parseResult.GetValue(pipelineIdArg),
                Branch = parseResult.GetValue(branchOption),
                Parameters = parameters,
                StagesToSkip = parseResult.GetValue(skipStageOption) ?? [],
            };

            return CommandHelpers.RunAsync(
                ct => PipelineTriggerAction.RunAsync(serviceProvider.GetRequiredService<IAzureDevOpsBuildClient>(), request, ct),
                CommandHelpers.Context.PipelineRunResult,
                cancellationToken);
        });

        return command;
    }
}
