using System.CommandLine;
using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Quill.AzureDevOps;
using Quill.Core;
using Quill.Core.Models;

namespace Quill.Cli.Commands.Pr;

internal static class ChecksCommand
{
    public static Command Create(IServiceProvider serviceProvider)
    {
        var idArg = new Argument<int>("id") { Description = "The Azure DevOps pull request ID" };

        idArg.Validators.Add(result =>
        {
            if (result.Tokens.Count > 0 && int.TryParse(result.Tokens[0].Value, CultureInfo.InvariantCulture, out var id) && id < 1)
            {
                result.AddError("id must be a positive integer.");
            }
        });

        var command = new Command("checks", "Print the policy evaluations that gate a pull request as JSON")
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
            if (string.IsNullOrEmpty(pullRequest.ProjectId))
            {
                throw new InvalidOperationException($"Pull request {id} has no project.");
            }

            var evaluations = await pullRequestClient.GetPolicyEvaluationsAsync(
                pullRequest.ProjectId, pullRequest.CodeReviewId, cancellationToken);

            var result = PullRequestChecksResultBuilder.Build(pullRequest, evaluations);

            Console.WriteLine(JsonSerializer.Serialize(result, CommandHelpers.Context.PullRequestChecksResult));
        }
        catch (Exception ex) when (ex is FileNotFoundException or InvalidOperationException or HttpRequestException)
        {
            CommandHelpers.HandleError(ex);
        }
    }
}
