using System.CommandLine;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Quill.AzureDevOps;
using Quill.Core;
using Quill.Core.Models;

namespace Quill.Cli.Commands.Pr;

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

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            await ExecuteAsync(
                serviceProvider,
                parseResult.GetValue(repoOption)!,
                parseResult.GetValue(sourceBranchOption)!,
                parseResult.GetValue(titleOption)!,
                parseResult.GetValue(targetBranchOption),
                parseResult.GetValue(descriptionFileOption),
                parseResult.GetValue(workItemOption) ?? [],
                cancellationToken);
        });

        return command;
    }

    private static async Task ExecuteAsync(
        IServiceProvider serviceProvider,
        string repo,
        string sourceBranch,
        string title,
        string? targetBranch,
        string? descriptionFile,
        IReadOnlyList<int> workItemIds,
        CancellationToken cancellationToken)
    {
        try
        {
            var description = await ReadDescriptionAsync(descriptionFile, cancellationToken);
            if (description is null && descriptionFile is not null)
            {
                Console.WriteLine(JsonSerializer.Serialize(
                    new ErrorResult { Error = $"File not found: {descriptionFile}", Code = 3 },
                    CommandHelpers.Context.ErrorResult));
                Environment.ExitCode = 3;
                return;
            }

            var pullRequestClient = serviceProvider.GetRequiredService<AzureDevOpsPullRequestClient>();

            var created = await pullRequestClient.CreateAsync(
                new PullRequestCreateRequest
                {
                    Repo = repo,
                    SourceBranch = sourceBranch,
                    Title = title,
                    TargetBranch = targetBranch,
                    Description = description,
                    WorkItemIds = workItemIds,
                },
                cancellationToken);

            // The author is not a reviewer and a draft gains no policy-required reviewer, so myVote
            // and myIsRequired are null whatever the current user is. That saves the identity call.
            var result = PullRequestViewResultBuilder.Build(
                created,
                currentUserId: string.Empty,
                markdownDescription: created.Description,
                workItems: null,
                threads: null);

            Console.WriteLine(JsonSerializer.Serialize(result, CommandHelpers.Context.PullRequestViewResult));
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
