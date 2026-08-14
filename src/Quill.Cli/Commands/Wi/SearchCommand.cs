using System.CommandLine;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Quill.AzureDevOps;
using Quill.Core;
using Quill.Core.Models;

namespace Quill.Cli.Commands.Wi;

internal static class SearchCommand
{
    public static Command Create(IServiceProvider serviceProvider)
    {
        var queryArg = new Argument<string?>("query")
        {
            Description = "Optional free text, matched against the work item title via WIQL CONTAINS WORDS.",
            Arity = ArgumentArity.ZeroOrOne,
        };

        var assigneeOption = new Option<string?>("--assignee")
        {
            Description = "Assignee filter. Accepts @me or a display name.",
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

        var command = new Command("search", "Search work items via WIQL and print matches as a JSON array")
        {
            queryArg,
            assigneeOption,
            stateOption,
            typeOption,
            limitOption,
        };

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var query = parseResult.GetValue(queryArg);
            var assignee = parseResult.GetValue(assigneeOption);
            var states = parseResult.GetValue(stateOption) ?? [];
            var types = parseResult.GetValue(typeOption) ?? [];
            var limit = parseResult.GetValue(limitOption);
            await ExecuteAsync(serviceProvider, query, assignee, states, types, limit, cancellationToken);
        });

        return command;
    }

    private static async Task ExecuteAsync(
        IServiceProvider serviceProvider,
        string? query,
        string? assignee,
        IReadOnlyList<string> states,
        IReadOnlyList<string> types,
        int limit,
        CancellationToken cancellationToken)
    {
        try
        {
            string wiql;
            int top;
            try
            {
                (wiql, top) = WiqlBuilder.Build(query, assignee, states, types, limit);
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine(JsonSerializer.Serialize(
                    new ErrorResult { Error = ex.Message, Code = 3 }, CommandHelpers.Context.ErrorResult));
                Environment.ExitCode = 3;
                return;
            }

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
