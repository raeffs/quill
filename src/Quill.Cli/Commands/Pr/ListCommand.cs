using System.CommandLine;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Quill.AzureDevOps;
using Quill.Core;
using Quill.Core.Models;

namespace Quill.Cli.Commands.Pr;

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

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var reviewer = parseResult.GetValue(reviewerOption);
            var author = parseResult.GetValue(authorOption);
            var state = parseResult.GetValue(stateOption);
            var repo = parseResult.GetValue(repoOption);
            var sourceBranch = parseResult.GetValue(sourceBranchOption);
            var targetBranch = parseResult.GetValue(targetBranchOption);
            var limit = parseResult.GetValue(limitOption);
            var skip = parseResult.GetValue(skipOption);
            await ExecuteAsync(
                serviceProvider, reviewer, author, state, repo, sourceBranch, targetBranch, limit, skip, cancellationToken);
        });

        return command;
    }

    private static async Task ExecuteAsync(
        IServiceProvider serviceProvider,
        string? reviewer,
        string? author,
        string? state,
        string? repo,
        string? sourceBranch,
        string? targetBranch,
        int limit,
        int? skip,
        CancellationToken cancellationToken)
    {
        try
        {
            if (reviewer is not null && !string.Equals(reviewer, "@me", StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "--reviewer accepts only '@me' in this release.");
            }

            if (author is not null && !string.Equals(author, "@me", StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "--author accepts only '@me' in this release.");
            }

            if (skip is < 0)
            {
                throw new InvalidOperationException("--skip must be zero or greater.");
            }

            var effectiveState = string.IsNullOrEmpty(state) ? "active" : state;
            if (!AllowedStates.Contains(effectiveState, StringComparer.Ordinal))
            {
                throw new InvalidOperationException(
                    $"--state must be one of: {string.Join(", ", AllowedStates)}.");
            }

            var identityClient = serviceProvider.GetRequiredService<AzureDevOpsIdentityClient>();
            var pullRequestClient = serviceProvider.GetRequiredService<AzureDevOpsPullRequestClient>();

            var currentUser = await identityClient.GetCurrentUserAsync();
            var userId = currentUser.Id;

            var creatorId = string.Equals(author, "@me", StringComparison.Ordinal) ? userId : null;
            var reviewerId = string.Equals(reviewer, "@me", StringComparison.Ordinal) ? userId : null;

            var query = new PullRequestListQuery
            {
                CreatorId = creatorId,
                ReviewerId = reviewerId,
                Status = effectiveState,
                Repo = repo,
                SourceBranch = sourceBranch,
                TargetBranch = targetBranch,
                Top = limit,
                Skip = skip,
            };

            var pullRequests = await pullRequestClient.ListAsync(query, cancellationToken);

            var results = pullRequests
                .Select(pr => PullRequestResultBuilder.Build(pr, userId))
                .ToList();

            Console.WriteLine(JsonSerializer.Serialize(results, CommandHelpers.Context.ListPullRequestResult));
        }
        catch (Exception ex) when (ex is FileNotFoundException or InvalidOperationException or HttpRequestException)
        {
            CommandHelpers.HandleError(ex);
        }
    }
}
