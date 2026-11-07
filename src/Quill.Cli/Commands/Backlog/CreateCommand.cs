using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quill.Core;
using Quill.Core.Actions;
using Quill.Core.Models;

namespace Quill.Cli.Commands.Backlog;

internal static class CreateCommand
{
    public static Command Create(IServiceProvider serviceProvider)
    {
        var fileArg = new Argument<string>("file-path") { Description = "Path to the markdown file with frontmatter" };
        var assignedToOption = new Option<string?>("--assigned-to") { Description = "Name of the user to assign the work item to. Defaults to the authenticated PAT user." };

        var command = new Command("create", "Create a new work item on Azure DevOps from a markdown file")
        {
            fileArg,
            assignedToOption,
        };

        command.SetAction((parseResult, cancellationToken) =>
        {
            var filePath = parseResult.GetValue(fileArg)!;
            var assignedTo = parseResult.GetValue(assignedToOption);
            return ExecuteAsync(serviceProvider, filePath, assignedTo, cancellationToken);
        });

        return command;
    }

    private static Task ExecuteAsync(
        IServiceProvider serviceProvider, string filePath, string? assignedTo, CancellationToken cancellationToken)
    {
        return CommandHelpers.RunAsync(
            async ct =>
            {
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException($"File not found: {filePath}");
                }

                var content = await File.ReadAllTextAsync(filePath, ct);

                var outcome = await BacklogCreateAction.RunAsync(
                    serviceProvider.GetRequiredService<IAzureDevOpsClient>(),
                    serviceProvider.GetRequiredService<IIdentityClient>(),
                    serviceProvider.GetRequiredService<QuillConfig>(),
                    serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Quill.Create"),
                    new BacklogCreateRequest(content, assignedTo));

                await File.WriteAllTextAsync(filePath, outcome.FileContent, CancellationToken.None);

                return outcome.Row;
            },
            CommandHelpers.Context.CreateResult,
            cancellationToken);
    }
}
