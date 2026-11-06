using System.CommandLine;
using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Quill.AzureDevOps;
using Quill.Core;
using Quill.Core.Models;

namespace Quill.Cli.Commands.Pipeline;

internal static class ListCommand
{
    public static Command Create(IServiceProvider serviceProvider)
    {
        var nameOption = new Option<string?>("--name")
        {
            Description = "Pipeline name filter. The server matches the pattern.",
        };

        var folderOption = new Option<string?>("--folder")
        {
            Description = "Folder filter, for example \\Team A.",
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

        var command = new Command("list", "List pipelines and print them as a JSON array")
        {
            nameOption,
            folderOption,
            limitOption,
        };

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var query = new PipelineListQuery
            {
                Name = parseResult.GetValue(nameOption),
                Folder = parseResult.GetValue(folderOption),
                Top = parseResult.GetValue(limitOption),
            };
            await ExecuteAsync(serviceProvider, query, cancellationToken);
        });

        return command;
    }

    private static async Task ExecuteAsync(
        IServiceProvider serviceProvider,
        PipelineListQuery query,
        CancellationToken cancellationToken)
    {
        try
        {
            var buildClient = serviceProvider.GetRequiredService<AzureDevOpsBuildClient>();

            var pipelines = await buildClient.ListPipelinesAsync(query, cancellationToken);
            var results = pipelines.Select(PipelineResultBuilder.Build).ToList();

            Console.WriteLine(JsonSerializer.Serialize(results, CommandHelpers.Context.ListPipelineResult));
        }
        catch (Exception ex) when (ex is FileNotFoundException or InvalidOperationException or HttpRequestException)
        {
            CommandHelpers.HandleError(ex);
        }
    }
}
