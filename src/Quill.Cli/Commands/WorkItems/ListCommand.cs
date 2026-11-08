using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Quill.Core;
using Quill.Core.Actions.WorkItems;

namespace Quill.Cli.Commands.WorkItems;

internal static class ListCommand
{
    public static Command Create(IServiceProvider serviceProvider)
    {
        var titleOption = new Option<string?>("--title")
        {
            Description = "Free text, matched against the work item title via WIQL CONTAINS WORDS.",
        };

        var assigneeOption = new Option<string?>("--assignee")
        {
            Description = "Assignee filter. Accepts @me, a display name, a unique name or an email address.",
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

        var command = new Command("list", "Query work items via WIQL and print matches as a JSON array")
        {
            titleOption,
            assigneeOption,
            stateOption,
            typeOption,
            limitOption,
        };

        command.SetAction((parseResult, cancellationToken) =>
        {
            var request = new WiListRequest(
                parseResult.GetValue(titleOption),
                parseResult.GetValue(assigneeOption),
                parseResult.GetValue(stateOption) ?? [],
                parseResult.GetValue(typeOption) ?? [],
                parseResult.GetValue(limitOption));

            return CommandHelpers.RunAsync(
                ct => ListAction.RunAsync(
                    serviceProvider.GetRequiredService<IAzureDevOpsClient>(),
                    serviceProvider.GetRequiredService<IWorkItemBatchFetcher>(),
                    serviceProvider.GetRequiredService<IIdentityClient>(),
                    request,
                    ct),
                CommandHelpers.Context.ListSearchResult,
                cancellationToken);
        });

        return command;
    }
}
