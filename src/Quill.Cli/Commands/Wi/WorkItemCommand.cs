using System.CommandLine;

namespace Quill.Cli.Commands.Wi;

internal static class WorkItemCommand
{
    public static Command Create(IServiceProvider serviceProvider)
    {
        var command = new Command("wi", "Work item commands")
        {
            ViewCommand.Create(serviceProvider),
            CreateCommand.Create(serviceProvider),
            TreeCommand.Create(serviceProvider),
            CommentsCommand.Create(serviceProvider),
            ListCommand.Create(serviceProvider),
        };

        return command;
    }
}
