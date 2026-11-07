using System.CommandLine;

namespace Quill.Cli.Commands.PullRequests;

internal static class PrCommand
{
    public static Command Create(IServiceProvider serviceProvider)
    {
        var command = new Command("pr", "Pull request commands")
        {
            ChecksCommand.Create(serviceProvider),
            CreateCommand.Create(serviceProvider),
            ListCommand.Create(serviceProvider),
            RevisionsCommand.Create(serviceProvider),
            ThreadsCommand.Create(serviceProvider),
            ViewCommand.Create(serviceProvider),
        };

        return command;
    }
}
