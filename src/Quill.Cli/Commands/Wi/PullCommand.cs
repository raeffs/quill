using System.CommandLine;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quill.AzureDevOps;
using Quill.Cli;
using Quill.Core.Markdown;
using Quill.Core.Models;

namespace Quill.Cli.Commands.Wi;

internal static class PullCommand
{
    public static Command Create(IServiceProvider serviceProvider)
    {
        var idArg = new Argument<int>("work-item-id") { Description = "The Azure DevOps work item ID to pull" };
        var fileArg = new Argument<string>("file-path") { Description = "Path to write the markdown file" };

        var command = new Command("pull", "Pull a work item from Azure DevOps to a local markdown file")
        {
            idArg,
            fileArg,
        };

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var id = parseResult.GetValue(idArg);
            var filePath = parseResult.GetValue(fileArg)!;
            await ExecuteAsync(serviceProvider, id, filePath);
        });

        return command;
    }

    private static async Task ExecuteAsync(IServiceProvider serviceProvider, int id, string filePath)
    {
        try
        {
            var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Quill.Pull");
            var config = serviceProvider.GetRequiredService<QuillConfig>();

            var client = serviceProvider.GetRequiredService<AzureDevOpsClient>();

            var workItem = await client.GetWorkItemAsync(id);

            var markdownBody = string.IsNullOrEmpty(workItem.Description)
                ? string.Empty
                : await MarkdownConverter.ToMarkdownAsync(
                    workItem.Description, config.ServerUrl, config.Collection, config.Project, client, logger);

            var fileContent = FrontmatterParser.Write(
                id: workItem.Id,
                type: workItem.Type,
                title: workItem.Title,
                state: workItem.State,
                body: markdownBody.TrimEnd(),
                parentId: workItem.ParentId);

            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await File.WriteAllTextAsync(filePath, fileContent);

            Console.WriteLine(JsonSerializer.Serialize(
                new PullResult
                {
                    Id = workItem.Id,
                    Title = workItem.Title,
                    File = filePath,
                },
                CommandHelpers.Context.PullResult));
        }
        catch (Exception ex) when (ex is FileNotFoundException or InvalidOperationException or HttpRequestException)
        {
            CommandHelpers.HandleError(ex);
        }
    }
}
