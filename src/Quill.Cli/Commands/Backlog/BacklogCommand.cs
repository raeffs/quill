using System.CommandLine;

namespace Quill.Cli.Commands.Backlog;

internal static class BacklogCommand
{
    public static Command Create(IServiceProvider serviceProvider)
    {
        var command = new Command("backlog", "Backlog commands: maintain work items as markdown files")
        {
            PullCommand.Create(serviceProvider),
            PushCommand.Create(serviceProvider),
            CreateCommand.Create(serviceProvider),
        };

        return command;
    }
}
