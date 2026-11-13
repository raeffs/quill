using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Quill.Core;
using Quill.Core.Actions.WorkItems;
using Quill.Core.Markdown;
using Quill.Core.Models;
using Quill.Core.Validation;

namespace Quill.Cli.Commands.WorkItems;

internal static class CreateCommand
{
    private const string StdinPath = "-";

    public static Command Create(IServiceProvider serviceProvider)
    {
        var typeOption = new Option<string>("--type")
        {
            Description = $"Work item type: {string.Join(", ", WorkItemHierarchy.ChildTypes)}. Case-insensitive.",
            Required = true,
        };
        typeOption.Validators.Add(result =>
        {
            var value = result.GetValueOrDefault<string>();
            if (value is not null && WorkItemHierarchy.CanonicalChildType(value) is null)
            {
                result.AddError($"--type must be one of: {string.Join(", ", WorkItemHierarchy.ChildTypes)}.");
            }
        });

        var titleOption = new Option<string>("--title")
        {
            Description = "Title for the new work item.",
            Required = true,
        };

        var parentOption = new Option<int>("--parent")
        {
            Description = "The parent work item ID.",
            Required = true,
        };

        var descriptionFileOption = new Option<string?>("--description-file")
        {
            Description = "Path to a markdown file with the description. Pass - to read stdin.",
        };

        var assigneeOption = new Option<string?>("--assignee")
        {
            Description = "Person to assign the work item to. Accepts @me, a display name, a unique name or an email address. Defaults to unassigned.",
        };

        var tagOption = new Option<string[]>("--tag")
        {
            Description = "Tag for the new work item. Repeat for more than one.",
            AllowMultipleArgumentsPerToken = false,
        };

        var predecessorOption = new Option<int[]>("--predecessor")
        {
            Description = "ID of a work item that must be done before this one. Repeat for more than one.",
            AllowMultipleArgumentsPerToken = false,
        };

        var command = new Command("create", "Create a work item under a parent and print it to stdout as JSON")
        {
            typeOption,
            titleOption,
            parentOption,
            descriptionFileOption,
            assigneeOption,
            tagOption,
            predecessorOption,
        };

        command.SetAction((parseResult, cancellationToken) =>
        {
            return ExecuteAsync(
                serviceProvider,
                WorkItemHierarchy.CanonicalChildType(parseResult.GetValue(typeOption)!)!,
                parseResult.GetValue(titleOption)!,
                parseResult.GetValue(parentOption),
                parseResult.GetValue(descriptionFileOption),
                parseResult.GetValue(assigneeOption),
                parseResult.GetValue(tagOption) ?? [],
                parseResult.GetValue(predecessorOption) ?? [],
                cancellationToken);
        });

        return command;
    }

    private static Task ExecuteAsync(
        IServiceProvider serviceProvider,
        string type,
        string title,
        int parentId,
        string? descriptionFile,
        string? assignee,
        IReadOnlyList<string> tags,
        IReadOnlyList<int> predecessors,
        CancellationToken cancellationToken)
    {
        return CommandHelpers.RunAsync(
            async ct =>
            {
                var normalizedTags = WorkItemTags.Normalize(tags);
                var validatedPredecessors = WorkItemPredecessors.Validate(predecessors);

                var description = await ReadDescriptionAsync(descriptionFile, ct);
                if (description is null && descriptionFile is not null)
                {
                    throw new UsageException($"File not found: {descriptionFile}");
                }

                return await CreateAction.RunAsync(
                    serviceProvider.GetRequiredService<IAzureDevOpsClient>(),
                    serviceProvider.GetRequiredService<IIdentityClient>(),
                    serviceProvider.GetRequiredService<RichText>(),
                    new WorkItemCreateRequest(type, title, parentId, description, assignee, normalizedTags, validatedPredecessors));
            },
            CommandHelpers.Context.ViewResult,
            cancellationToken);
    }

    private static async Task<string?> ReadDescriptionAsync(string? path, CancellationToken cancellationToken)
    {
        if (path is null)
        {
            return null;
        }

        if (string.Equals(path, StdinPath, StringComparison.Ordinal))
        {
            return await Console.In.ReadToEndAsync(cancellationToken);
        }

        return File.Exists(path) ? await File.ReadAllTextAsync(path, cancellationToken) : null;
    }
}
