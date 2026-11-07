using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Quill.Core;
using Quill.Core.Actions;

namespace Quill.Cli.Commands.Wi;

internal static class TreeCommand
{
    public static Command Create(IServiceProvider serviceProvider)
    {
        var idArg = new Argument<int>("id") { Description = "Work item id to use as the subtree root" };

        var depthOption = new Option<int>("--depth")
        {
            Description = "How many levels below the root to fetch (≥ 1). Default: 3.",
            DefaultValueFactory = _ => 3,
        };

        var allOption = new Option<bool>("--all")
        {
            Description = "Traverse the entire subtree. Overrides --depth.",
        };

        var command = new Command("tree", "Print the hierarchy under a work item as nested JSON")
        {
            idArg,
            depthOption,
            allOption,
        };

        command.Validators.Add(result =>
        {
            if (!result.GetValue(allOption) && result.GetValue(depthOption) < 1)
            {
                result.AddError("--depth must be ≥ 1.");
            }
        });

        command.SetAction((parseResult, cancellationToken) =>
        {
            var id = parseResult.GetValue(idArg);
            var depth = parseResult.GetValue(depthOption);
            var all = parseResult.GetValue(allOption);
            int? maxDepth = all ? null : depth;

            return CommandHelpers.RunAsync(
                ct => WiTreeAction.RunAsync(
                    serviceProvider.GetRequiredService<IAzureDevOpsClient>(),
                    serviceProvider.GetRequiredService<IWorkItemBatchFetcher>(),
                    id,
                    maxDepth,
                    ct),
                CommandHelpers.Context.TreeNode,
                cancellationToken);
        });

        return command;
    }
}
