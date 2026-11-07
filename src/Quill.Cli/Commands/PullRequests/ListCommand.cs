using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Quill.Core;
using Quill.Core.Actions.PullRequests;

namespace Quill.Cli.Commands.PullRequests;

internal static class ListCommand
{
    private static readonly string[] AllowedStates = ["active", "completed", "abandoned", "all"];

    public static Command Create(IServiceProvider serviceProvider)
    {
        var reviewerOption = new Option<string?>("--reviewer")
        {
            Description = "Reviewer filter. Only @me is accepted in this release.",
        };

        var authorOption = new Option<string?>("--author")
        {
            Description = "Author filter. Only @me is accepted in this release.",
        };

        var stateOption = new Option<string?>("--state")
        {
            Description = "State filter. One of: active, completed, abandoned, all. Default: active.",
            DefaultValueFactory = _ => "active",
        };

        var repoOption = new Option<string?>("--repo")
        {
            Description = "Filter to a single repository by display name.",
        };

        var sourceBranchOption = new Option<string?>("--source-branch")
        {
            Description = "Source branch filter. Short name or full ref. Matches exactly.",
        };

        var targetBranchOption = new Option<string?>("--target-branch")
        {
            Description = "Target branch filter. Short name or full ref. Matches exactly.",
        };

        var limitOption = new Option<int>("--limit")
        {
            Description = "Maximum number of results. Default: 50.",
            DefaultValueFactory = _ => 50,
        };

        var skipOption = new Option<int?>("--skip")
        {
            Description = "Skip this many results. Use with --limit to page.",
        };

        Validators.NotNegative(skipOption, "--skip");
        AcceptOnlyMe(reviewerOption, "--reviewer");
        AcceptOnlyMe(authorOption, "--author");
        stateOption.Validators.Add(result =>
        {
            var value = result.GetValueOrDefault<string?>();
            if (!AllowedStates.Contains(value, StringComparer.Ordinal))
            {
                result.AddError($"--state must be one of: {string.Join(", ", AllowedStates)}.");
            }
        });

        var command = new Command("list", "List Azure DevOps pull requests and print matches as a JSON array")
        {
            reviewerOption,
            authorOption,
            stateOption,
            repoOption,
            sourceBranchOption,
            targetBranchOption,
            limitOption,
            skipOption,
        };

        command.SetAction((parseResult, cancellationToken) =>
        {
            var request = new PrListRequest(
                ReviewerIsMe: parseResult.GetValue(reviewerOption) is not null,
                AuthorIsMe: parseResult.GetValue(authorOption) is not null,
                State: parseResult.GetValue(stateOption)!,
                Repo: parseResult.GetValue(repoOption),
                SourceBranch: parseResult.GetValue(sourceBranchOption),
                TargetBranch: parseResult.GetValue(targetBranchOption),
                Limit: parseResult.GetValue(limitOption),
                Skip: parseResult.GetValue(skipOption));

            return CommandHelpers.RunAsync(
                ct => ListAction.RunAsync(
                    serviceProvider.GetRequiredService<IAzureDevOpsPullRequestClient>(),
                    serviceProvider.GetRequiredService<IIdentityClient>(),
                    request,
                    ct),
                CommandHelpers.Context.ListPullRequestResult,
                cancellationToken);
        });

        return command;
    }

    private static void AcceptOnlyMe(Option<string?> option, string name) =>
        option.Validators.Add(result =>
        {
            var value = result.GetValueOrDefault<string?>();
            if (value is not null && !string.Equals(value, "@me", StringComparison.Ordinal))
            {
                result.AddError($"{name} accepts only '@me' in this release.");
            }
        });
}
