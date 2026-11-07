using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quill.Cli;
using Quill.Core;
using Quill.Core.Actions;
using Quill.Core.Models;

namespace Quill.Cli.Commands.Backlog;

internal static class PushCommand
{
    public static Command Create(IServiceProvider serviceProvider)
    {
        var fileArg = new Argument<string>("file-path") { Description = "Path to the markdown file to push" };

        var command = new Command("push", "Push a local markdown file to Azure DevOps")
        {
            fileArg,
        };

        command.SetAction((parseResult, cancellationToken) =>
        {
            var filePath = parseResult.GetValue(fileArg)!;
            return ExecuteAsync(serviceProvider, filePath, cancellationToken);
        });

        return command;
    }

    private static Task ExecuteAsync(
        IServiceProvider serviceProvider, string filePath, CancellationToken cancellationToken)
    {
        return CommandHelpers.RunAsync(
            async ct =>
            {
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException($"File not found: {filePath}");
                }

                var content = await File.ReadAllTextAsync(filePath, ct);

                return await BacklogPushAction.RunAsync(
                    serviceProvider.GetRequiredService<IAzureDevOpsClient>(),
                    serviceProvider.GetRequiredService<IIdentityClient>(),
                    serviceProvider.GetRequiredService<QuillConfig>(),
                    serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Quill.Push"),
                    content);
            },
            CommandHelpers.Context.PushResult,
            cancellationToken);
    }
}
