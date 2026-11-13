using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Quill.Cli;
using Quill.Core;
using Quill.Core.Actions.Backlog;
using Quill.Core.Markdown;
using Quill.Core.Models;

namespace Quill.Cli.Commands.Backlog;

internal static class PullCommand
{
    public static Command Create(IServiceProvider serviceProvider)
    {
        var idArg = new Argument<int>("work-item-id") { Description = "The Azure DevOps work item ID to pull" };
        var fileArg = new Argument<string>("file-path") { Description = "Path to write the markdown file" };

        Validators.PositiveInteger(idArg, "work-item-id");

        var command = new Command("pull", "Pull a work item from Azure DevOps to a local markdown file")
        {
            idArg,
            fileArg,
        };

        command.SetAction((parseResult, cancellationToken) =>
        {
            var id = parseResult.GetValue(idArg);
            var filePath = parseResult.GetValue(fileArg)!;
            return ExecuteAsync(serviceProvider, id, filePath, cancellationToken);
        });

        return command;
    }

    private static Task ExecuteAsync(
        IServiceProvider serviceProvider, int id, string filePath, CancellationToken cancellationToken)
    {
        return CommandHelpers.RunAsync(
            async ct =>
            {
                var outcome = await PullAction.RunAsync(
                    serviceProvider.GetRequiredService<IAzureDevOpsClient>(),
                    serviceProvider.GetRequiredService<RichText>(),
                    id);

                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                await File.WriteAllTextAsync(filePath, outcome.FileContent, ct);

                return new PullResult { Id = outcome.Id, Title = outcome.Title, File = filePath };
            },
            CommandHelpers.Context.PullResult,
            cancellationToken);
    }
}
