using System.CommandLine;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Quill.AzureDevOps;
using Quill.Core;
using Quill.Core.Models;

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

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var id = parseResult.GetValue(idArg);
            var depth = parseResult.GetValue(depthOption);
            var all = parseResult.GetValue(allOption);
            await ExecuteAsync(serviceProvider, id, depth, all, cancellationToken);
        });

        return command;
    }

    private static async Task ExecuteAsync(IServiceProvider serviceProvider, int id, int depth, bool all, CancellationToken cancellationToken)
    {
        try
        {
            if (!all && depth < 1)
            {
                Console.WriteLine(JsonSerializer.Serialize(
                    new ErrorResult { Error = "--depth must be ≥ 1.", Code = 3 }, CommandHelpers.Context.ErrorResult));
                Environment.ExitCode = 3;
                return;
            }

            var client = serviceProvider.GetRequiredService<AzureDevOpsClient>();

            var root = await client.GetWorkItemAsync(id);

            int? maxDepth = all ? null : depth;
            var tree = await TreeBuilder.BuildAsync(root, client, maxDepth, cancellationToken);

            Console.WriteLine(JsonSerializer.Serialize(tree, CommandHelpers.Context.TreeNode));
        }
        catch (Exception ex) when (ex is FileNotFoundException or InvalidOperationException or HttpRequestException)
        {
            CommandHelpers.HandleError(ex);
        }
    }
}
