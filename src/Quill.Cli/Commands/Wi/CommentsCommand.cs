using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quill.Core;
using Quill.Core.Actions.Wi;
using Quill.Core.Models;

namespace Quill.Cli.Commands.Wi;

internal static class CommentsCommand
{
    public static Command Create(IServiceProvider serviceProvider)
    {
        var idArg = new Argument<int>("id") { Description = "The Azure DevOps work item ID whose comment thread to read" };

        var limitOption = new Option<int?>("--limit")
        {
            Description = "Return only the N most recent comments (must be >= 1). Omit to return all comments.",
        };
        Validators.PositiveInteger(limitOption, "--limit");

        var command = new Command("comments", "Print the comment thread on a work item as JSON, newest first")
        {
            idArg,
            limitOption,
        };

        command.SetAction((parseResult, cancellationToken) =>
        {
            var id = parseResult.GetValue(idArg);
            var limit = parseResult.GetValue(limitOption);

            return CommandHelpers.RunAsync(
                ct => CommentsAction.RunAsync(
                    serviceProvider.GetRequiredService<IAzureDevOpsClient>(),
                    serviceProvider.GetRequiredService<QuillConfig>(),
                    serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Quill.Comments"),
                    id,
                    limit,
                    ct),
                CommandHelpers.Context.ListCommentResult,
                cancellationToken);
        });

        return command;
    }
}
