using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Quill.Core;
using Quill.Core.Actions.Pipelines;
using Quill.Core.Models;

namespace Quill.Cli.Commands.Pipeline;

internal static class ListCommand
{
    public static Command Create(IServiceProvider serviceProvider)
    {
        var nameOption = new Option<string?>("--name")
        {
            Description = "Pipeline name. Matches the whole name; use * as a wildcard, for example \"Release*\".",
        };

        var folderOption = new Option<string?>("--folder")
        {
            Description = "Folder path with a leading backslash, for example \\Team A. Lists that folder only, not its subfolders.",
        };

        var limitOption = new Option<int>("--limit")
        {
            Description = "Maximum number of results. Default: 50.",
            DefaultValueFactory = _ => 50,
        };

        Validators.PositiveInteger(limitOption, "--limit");

        var command = new Command("list", "List pipelines and print them as a JSON array")
        {
            nameOption,
            folderOption,
            limitOption,
        };

        command.SetAction((parseResult, cancellationToken) =>
        {
            var query = new PipelineListQuery
            {
                Name = parseResult.GetValue(nameOption),
                Folder = parseResult.GetValue(folderOption),
                Top = parseResult.GetValue(limitOption),
            };

            return CommandHelpers.RunAsync(
                ct => ListAction.RunAsync(serviceProvider.GetRequiredService<IAzureDevOpsBuildClient>(), query, ct),
                CommandHelpers.Context.ListPipelineResult,
                cancellationToken);
        });

        return command;
    }
}
