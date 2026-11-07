using System.CommandLine;

namespace Quill.Cli.Commands.Pipelines;

internal static class PipelineCommand
{
    public static Command Create(IServiceProvider serviceProvider)
    {
        var command = new Command("pipeline", "Pipeline commands")
        {
            ListCommand.Create(serviceProvider),
            RunsCommand.Create(serviceProvider),
            ViewCommand.Create(serviceProvider),
            LogCommand.Create(serviceProvider),
            TriggerCommand.Create(serviceProvider),
            WaitCommand.Create(serviceProvider),
            CancelCommand.Create(serviceProvider),
            RetryCommand.Create(serviceProvider),
            TestsCommand.Create(serviceProvider),
        };

        return command;
    }
}
