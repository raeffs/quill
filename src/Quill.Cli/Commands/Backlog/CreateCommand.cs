using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Quill.Core;
using Quill.Core.Actions.Backlog;
using Quill.Core.Markdown;
using Quill.Core.Models;

namespace Quill.Cli.Commands.Backlog;

internal static class CreateCommand
{
    public static Command Create(IServiceProvider serviceProvider)
    {
        var fileArg = new Argument<string>("file-path") { Description = "Path to the markdown file with frontmatter" };
        var assigneeOption = new Option<string?>("--assignee") { Description = "Person to assign the work item to. Accepts @me, a display name, a unique name or an email address. Defaults to @me." };

        var command = new Command("create", "Create a new work item on Azure DevOps from a markdown file")
        {
            fileArg,
            assigneeOption,
        };

        command.SetAction((parseResult, cancellationToken) =>
        {
            var filePath = parseResult.GetValue(fileArg)!;
            var assignee = parseResult.GetValue(assigneeOption);
            return ExecuteAsync(serviceProvider, filePath, assignee, cancellationToken);
        });

        return command;
    }

    private static Task ExecuteAsync(
        IServiceProvider serviceProvider, string filePath, string? assignee, CancellationToken cancellationToken)
    {
        return CommandHelpers.RunAsync(
            async ct =>
            {
                if (!File.Exists(filePath))
                {
                    throw new UsageException($"File not found: {filePath}");
                }

                var content = await File.ReadAllTextAsync(filePath, ct);

                var outcome = await CreateAction.RunAsync(
                    serviceProvider.GetRequiredService<IAzureDevOpsClient>(),
                    serviceProvider.GetRequiredService<IIdentityClient>(),
                    serviceProvider.GetRequiredService<RichText>(),
                    content,
                    assignee);

                await File.WriteAllTextAsync(filePath, outcome.FileContent, CancellationToken.None);

                return outcome.Row;
            },
            CommandHelpers.Context.CreateResult,
            cancellationToken);
    }
}
