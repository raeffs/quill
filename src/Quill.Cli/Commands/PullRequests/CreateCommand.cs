using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Quill.Core;
using Quill.Core.Actions.PullRequests;
using Quill.Core.Models;

namespace Quill.Cli.Commands.PullRequests;

internal static class CreateCommand
{
    private const string StdinPath = "-";

    public static Command Create(IServiceProvider serviceProvider)
    {
        var repoOption = new Option<string>("--repo")
        {
            Description = "Repository display name.",
            Required = true,
        };

        var sourceBranchOption = new Option<string>("--source-branch")
        {
            Description = "Branch to merge from. Short name or full ref.",
            Required = true,
        };

        var titleOption = new Option<string>("--title")
        {
            Description = "Pull request title.",
            Required = true,
        };

        var targetBranchOption = new Option<string?>("--target-branch")
        {
            Description = "Branch to merge into. Short name or full ref. Defaults to the repository's default branch.",
        };

        var descriptionFileOption = new Option<string?>("--description-file")
        {
            Description = "Path to a markdown file with the description. Pass - to read stdin.",
        };

        var workItemOption = new Option<int[]>("--work-item")
        {
            Description = "Work item ID to link. Repeat for more than one.",
            AllowMultipleArgumentsPerToken = false,
        };

        var command = new Command("create", "Open a pull request as a draft and print it to stdout as JSON")
        {
            repoOption,
            sourceBranchOption,
            titleOption,
            targetBranchOption,
            descriptionFileOption,
            workItemOption,
        };

        command.SetAction((parseResult, cancellationToken) =>
        {
            var repo = parseResult.GetValue(repoOption)!;
            var sourceBranch = parseResult.GetValue(sourceBranchOption)!;
            var title = parseResult.GetValue(titleOption)!;
            var targetBranch = parseResult.GetValue(targetBranchOption);
            var descriptionFile = parseResult.GetValue(descriptionFileOption);
            var workItemIds = parseResult.GetValue(workItemOption) ?? [];

            return CommandHelpers.RunAsync(
                async ct =>
                {
                    var description = await ReadDescriptionAsync(descriptionFile, ct);
                    if (description is null && descriptionFile is not null)
                    {
                        throw new FileNotFoundException($"File not found: {descriptionFile}");
                    }

                    return await CreateAction.RunAsync(
                        serviceProvider.GetRequiredService<IAzureDevOpsPullRequestClient>(),
                        new PullRequestCreateRequest
                        {
                            Repo = repo,
                            SourceBranch = sourceBranch,
                            Title = title,
                            TargetBranch = targetBranch,
                            Description = description,
                            WorkItemIds = workItemIds,
                        },
                        ct);
                },
                CommandHelpers.Context.PullRequestViewResult,
                cancellationToken);
        });

        return command;
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
