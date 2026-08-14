using System.CommandLine;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quill.AzureDevOps;
using Quill.Core;
using Quill.Core.Markdown;
using Quill.Core.Models;

namespace Quill.Cli.Commands.Wi;

internal static class ViewCommand
{
    public static Command Create(IServiceProvider serviceProvider)
    {
        var idArg = new Argument<int>("id") { Description = "The Azure DevOps work item ID to view" };

        var markdownOption = new Option<bool>("--markdown")
        {
            Description = "Emit the same markdown document `pull` would write, to stdout.",
        };

        var withChildrenOption = new Option<bool>("--with-children")
        {
            Description = "Append a `children` array with each child's id, title, and state.",
        };

        var command = new Command("view", "Print a work item to stdout without writing to a file")
        {
            idArg,
            markdownOption,
            withChildrenOption,
        };

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var id = parseResult.GetValue(idArg);
            var markdown = parseResult.GetValue(markdownOption);
            var withChildren = parseResult.GetValue(withChildrenOption);
            await ExecuteAsync(serviceProvider, id, markdown, withChildren, cancellationToken);
        });

        return command;
    }

    private static async Task ExecuteAsync(
        IServiceProvider serviceProvider, int id, bool markdown, bool withChildren, CancellationToken cancellationToken)
    {
        try
        {
            if (markdown && withChildren)
            {
                throw new InvalidOperationException("--markdown and --with-children are mutually exclusive.");
            }

            var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Quill.View");
            var config = serviceProvider.GetRequiredService<QuillConfig>();

            var client = serviceProvider.GetRequiredService<AzureDevOpsClient>();

            var workItem = await client.GetWorkItemAsync(id);

            var body = string.IsNullOrEmpty(workItem.Description)
                ? string.Empty
                : (await MarkdownConverter.ToMarkdownAsync(
                    workItem.Description, config.ServerUrl, config.Collection, config.Project, client, logger)).TrimEnd();

            if (markdown)
            {
                var fileContent = FrontmatterParser.Write(
                    id: workItem.Id,
                    type: workItem.Type,
                    title: workItem.Title,
                    state: workItem.State,
                    body: body,
                    parentId: workItem.ParentId);
                Console.Write(fileContent);
                return;
            }

            IReadOnlyList<ChildItem>? children = null;
            if (withChildren)
            {
                var batch = await client.FetchAsync(workItem.ChildIds, cancellationToken);
                children = batch.Items
                    .Select(i => new ChildItem { Id = i.Id, Title = i.Title, State = i.State })
                    .ToList();
            }

            var result = ViewResultBuilder.Build(workItem, body, children);
            Console.WriteLine(JsonSerializer.Serialize(result, CommandHelpers.Context.ViewResult));
        }
        catch (Exception ex) when (ex is FileNotFoundException or InvalidOperationException or HttpRequestException)
        {
            CommandHelpers.HandleError(ex);
        }
    }
}
