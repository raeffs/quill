using System.CommandLine;

namespace Quill.Cli.Commands.Pipeline;

internal static class PipelineCommand
{
    public static Command Create(IServiceProvider serviceProvider)
    {
        var command = new Command("pipeline", "Pipeline commands")
        {
            ViewCommand.Create(serviceProvider),
        };

        return command;
    }
}
