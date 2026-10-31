using System.CommandLine;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quill.AzureDevOps;
using Quill.Core;
using Quill.Core.Models;
using Quill.Core.Validation;

namespace Quill.Cli.Commands.Wi;

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
            Description = "Name of the user to assign the work item to. Defaults to unassigned.",
        };

        var tagOption = new Option<string[]>("--tag")
        {
            Description = "Tag for the new work item. Repeat for more than one.",
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
        };

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            await ExecuteAsync(
                serviceProvider,
                WorkItemHierarchy.CanonicalChildType(parseResult.GetValue(typeOption)!)!,
                parseResult.GetValue(titleOption)!,
                parseResult.GetValue(parentOption),
                parseResult.GetValue(descriptionFileOption),
                parseResult.GetValue(assigneeOption),
                parseResult.GetValue(tagOption) ?? [],
                cancellationToken);
        });

        return command;
    }

    private static async Task ExecuteAsync(
        IServiceProvider serviceProvider,
        string type,
        string title,
        int parentId,
        string? descriptionFile,
        string? assignee,
        IReadOnlyList<string> tags,
        CancellationToken cancellationToken)
    {
        try
        {
            var normalizedTags = WorkItemTags.Normalize(tags);

            var description = await ReadDescriptionAsync(descriptionFile, cancellationToken);
            if (description is null && descriptionFile is not null)
            {
                Console.WriteLine(JsonSerializer.Serialize(
                    new ErrorResult { Error = $"File not found: {descriptionFile}", Code = 3 },
                    CommandHelpers.Context.ErrorResult));
                Environment.ExitCode = 3;
                return;
            }

            var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Quill.Create");
            var config = serviceProvider.GetRequiredService<QuillConfig>();
            var client = serviceProvider.GetRequiredService<AzureDevOpsClient>();

            var outcome = await WorkItemCreator.CreateAsync(
                client, config, new WorkItemCreateRequest(type, title, parentId, description, assignee, normalizedTags), logger);

            if (outcome.Created is null)
            {
                var errorMsg = string.Join("; ", outcome.Validation.Errors);
                Console.WriteLine(JsonSerializer.Serialize(
                    new ErrorResult { Error = errorMsg, Code = 1 }, CommandHelpers.Context.ErrorResult));
                Environment.ExitCode = 1;
                return;
            }

            Console.WriteLine(JsonSerializer.Serialize(outcome.Created, CommandHelpers.Context.ViewResult));
        }
        catch (Exception ex) when (ex is FileNotFoundException or InvalidOperationException or HttpRequestException)
        {
            CommandHelpers.HandleError(ex);
        }
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
