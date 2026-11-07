using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quill.Core;
using Quill.Core.Actions;
using Quill.Core.Models;

namespace Quill.Cli.Commands.Wi;

internal static class ViewCommand
{
    public static Command Create(IServiceProvider serviceProvider)
    {
        var idArg = new Argument<int>("id") { Description = "The Azure DevOps work item ID to view" };

        var withChildrenOption = new Option<bool>("--with-children")
        {
            Description = "Append a `children` array with each child's id, title, and state.",
        };

        var command = new Command("view", "Print a work item to stdout without writing to a file")
        {
            idArg,
            withChildrenOption,
        };

        command.SetAction((parseResult, cancellationToken) =>
        {
            var id = parseResult.GetValue(idArg);
            var withChildren = parseResult.GetValue(withChildrenOption);

            return CommandHelpers.RunAsync(
                ct => WiViewAction.RunAsync(
                    serviceProvider.GetRequiredService<IAzureDevOpsClient>(),
                    serviceProvider.GetRequiredService<IWorkItemBatchFetcher>(),
                    serviceProvider.GetRequiredService<QuillConfig>(),
                    serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Quill.View"),
                    id,
                    withChildren,
                    ct),
                CommandHelpers.Context.ViewResult,
                cancellationToken);
        });

        return command;
    }
}
