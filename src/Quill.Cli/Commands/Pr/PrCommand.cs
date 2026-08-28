using System.CommandLine;

namespace Quill.Cli.Commands.Pr;

internal static class PrCommand
{
    public static Command Create(IServiceProvider serviceProvider)
    {
        var command = new Command("pr", "Pull request commands")
        {
            CreateCommand.Create(serviceProvider),
            ListCommand.Create(serviceProvider),
            RevisionsCommand.Create(serviceProvider),
            ThreadsCommand.Create(serviceProvider),
            ViewCommand.Create(serviceProvider),
        };

        return command;
    }
}
