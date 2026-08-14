using System.CommandLine;

namespace Quill.Cli.Commands.Wi;

internal static class WorkItemCommand
{
    public static Command Create(IServiceProvider serviceProvider)
    {
        var command = new Command("wi", "Work item commands")
        {
            PushCommand.Create(serviceProvider),
            PullCommand.Create(serviceProvider),
            ViewCommand.Create(serviceProvider),
            CreateCommand.Create(serviceProvider),
            CreateTaskCommand.Create(serviceProvider),
            TreeCommand.Create(serviceProvider),
            SearchCommand.Create(serviceProvider),
            CommentsCommand.Create(serviceProvider),
            ListCommand.Create(serviceProvider),
        };

        return command;
    }
}
