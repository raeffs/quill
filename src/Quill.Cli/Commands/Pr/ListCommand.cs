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
            Description = "Reviewer filter. Only @me is accepted in this release. Defaults to @me when omitted.",
            DefaultValueFactory = _ => "@me",
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

        var includeDraftsOption = new Option<bool>("--include-drafts")
        {
            Description = "Include draft pull requests in the output. Drafts are filtered out by default.",
        };

        var limitOption = new Option<int>("--limit")
        {
            Description = "Maximum number of results. Default: 50.",
            DefaultValueFactory = _ => 50,
        };

        var command = new Command("list", "List Azure DevOps pull requests and print matches as a JSON array")
        {
            reviewerOption,
            authorOption,
            stateOption,
            repoOption,
            includeDraftsOption,
            limitOption,
        };

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var reviewer = parseResult.GetValue(reviewerOption);
            var author = parseResult.GetValue(authorOption);
            var state = parseResult.GetValue(stateOption);
            var repo = parseResult.GetValue(repoOption);
            var includeDrafts = parseResult.GetValue(includeDraftsOption);
            var limit = parseResult.GetValue(limitOption);
            await ExecuteAsync(serviceProvider, reviewer, author, state, repo, includeDrafts, limit, cancellationToken);
        });

        return command;
    }

    private static async Task ExecuteAsync(
        IServiceProvider serviceProvider,
        string? reviewer,
        string? author,
        string? state,
        string? repo,
        bool includeDrafts,
        int limit,
        CancellationToken cancellationToken)
    {
        try
        {
            if (reviewer is not null && !string.Equals(reviewer, "@me", StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "--reviewer accepts only '@me' in this release. Named-identity resolution is tracked in issue #99.");
            }

            if (author is not null && !string.Equals(author, "@me", StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "--author accepts only '@me' in this release. Named-identity resolution is tracked in issue #99.");
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

            var pullRequests = await pullRequestClient.ListAsync(
                creatorId, reviewerId, effectiveState, repo, limit, cancellationToken);

            IEnumerable<PullRequest> filtered = pullRequests;
            if (!includeDrafts)
            {
                filtered = filtered.Where(pr => !pr.IsDraft);
            }

            var results = filtered
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
