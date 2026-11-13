using System.CommandLine;
using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Quill.Core;
using Quill.Core.Actions.WorkItems;

namespace Quill.Cli.Commands.WorkItems;

internal static class TreeCommand
{
    public static Command Create(IServiceProvider serviceProvider)
    {
        var idArg = new Argument<int>("id") { Description = "Work item id to use as the subtree root" };

        Validators.PositiveInteger(idArg, "id");

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
            var tokens = result.GetResult(depthOption)?.Tokens;
            if (tokens is null || tokens.Count == 0 || result.GetValue(allOption))
            {
                return;
            }

            if (int.TryParse(tokens[0].Value, CultureInfo.InvariantCulture, out var depth) && depth < 1)
            {
                result.AddError("--depth must be at least 1.");
            }
        });

        command.SetAction((parseResult, cancellationToken) =>
        {
            var id = parseResult.GetValue(idArg);
            var depth = parseResult.GetValue(depthOption);
            var all = parseResult.GetValue(allOption);
            int? maxDepth = all ? null : depth;

            return CommandHelpers.RunAsync(
                ct => TreeAction.RunAsync(
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
