using System.CommandLine;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Quill.AzureDevOps;
using Quill.Core;

namespace Quill.Cli.Commands.Wi;

internal static class ListCommand
{
    public static Command Create(IServiceProvider serviceProvider)
    {
        var assigneeOption = new Option<string?>("--assignee")
        {
            Description = "Assignee filter. Accepts @me or a display name. Defaults to @me when omitted.",
            DefaultValueFactory = _ => "@me",
        };

        var stateOption = new Option<string[]>("--state")
        {
            Description = "State filter. Repeat to OR multiple values.",
            AllowMultipleArgumentsPerToken = false,
        };

        var typeOption = new Option<string[]>("--type")
        {
            Description = "Work item type filter. Repeat to OR multiple values.",
            AllowMultipleArgumentsPerToken = false,
        };

        var limitOption = new Option<int>("--limit")
        {
            Description = "Maximum number of results. Default: 50.",
            DefaultValueFactory = _ => 50,
        };

        var command = new Command("list", "List work items assigned to me (or another assignee) and print matches as a JSON array")
        {
            assigneeOption,
            stateOption,
            typeOption,
            limitOption,
        };

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var assignee = parseResult.GetValue(assigneeOption);
            var states = parseResult.GetValue(stateOption) ?? [];
            var types = parseResult.GetValue(typeOption) ?? [];
            var limit = parseResult.GetValue(limitOption);
            await ExecuteAsync(serviceProvider, assignee, states, types, limit, cancellationToken);
        });

        return command;
    }

    private static async Task ExecuteAsync(
        IServiceProvider serviceProvider,
        string? assignee,
        IReadOnlyList<string> states,
        IReadOnlyList<string> types,
        int limit,
        CancellationToken cancellationToken)
    {
        try
        {
            var (wiql, top) = WiqlBuilder.Build(null, assignee, states, types, limit);

            var client = serviceProvider.GetRequiredService<AzureDevOpsClient>();

            var ids = await client.QueryByWiqlAsync(wiql, top, cancellationToken);
            var batch = await client.FetchAsync(ids, cancellationToken);

            var byId = batch.Items.ToDictionary(i => i.Id);
            var results = ids
                .Where(byId.ContainsKey)
                .Select(id => SearchResultBuilder.Build(byId[id]))
                .ToList();

            Console.WriteLine(JsonSerializer.Serialize(results, CommandHelpers.Context.ListSearchResult));
        }
        catch (Exception ex) when (ex is FileNotFoundException or InvalidOperationException or HttpRequestException)
        {
            CommandHelpers.HandleError(ex);
        }
    }
}
