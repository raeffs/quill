using System.CommandLine;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Quill.AzureDevOps;
using Quill.Core;
using Quill.Core.Models;

namespace Quill.Cli.Commands.Pr;

internal static class RevisionsCommand
{
    public static Command Create(IServiceProvider serviceProvider)
    {
        var idArg = new Argument<int>("id") { Description = "The Azure DevOps pull request ID whose revisions to read" };

        var command = new Command("revisions", "Print a pull request's revisions as JSON, newest first")
        {
            idArg,
        };

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var id = parseResult.GetValue(idArg);
            await ExecuteAsync(serviceProvider, id, cancellationToken);
        });

        return command;
    }

    private static async Task ExecuteAsync(
        IServiceProvider serviceProvider,
        int id,
        CancellationToken cancellationToken)
    {
        try
        {
            var pullRequestClient = serviceProvider.GetRequiredService<AzureDevOpsPullRequestClient>();

            var pullRequest = await pullRequestClient.GetByIdAsync(id, cancellationToken);
            var revisions = await pullRequestClient.GetRevisionsAsync(id, pullRequest.RepoName, cancellationToken);

            var results = new List<PullRequestRevisionResult>(revisions.Count);
            foreach (var revision in revisions)
            {
                results.Add(PullRequestRevisionResultBuilder.Build(revision));
            }

            Console.WriteLine(JsonSerializer.Serialize(results, CommandHelpers.Context.ListPullRequestRevisionResult));
        }
        catch (Exception ex) when (ex is FileNotFoundException or InvalidOperationException or HttpRequestException)
        {
            CommandHelpers.HandleError(ex);
        }
    }
}
