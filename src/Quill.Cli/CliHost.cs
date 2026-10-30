using System.CommandLine;
using System.CommandLine.Parsing;
using Quill.Cli.Commands.Backlog;
using Quill.Cli.Commands.Pr;
using Quill.Cli.Commands.Wi;

namespace Quill.Cli;

internal static class CliHost
{
    // Response-file expansion treats any argument starting with `@` as a file path,
    // which swallows valid values like `--assignee @me`. Disable it globally.
    public static ParserConfiguration ParserConfiguration { get; } = new()
    {
        ResponseFileTokenReplacer = null,
    };

    public static RootCommand BuildRootCommand(IServiceProvider serviceProvider)
    {
        var rootCommand = new RootCommand("Quill — Azure DevOps Server CLI for coding agents");

        rootCommand.Subcommands.Add(BacklogCommand.Create(serviceProvider));
        rootCommand.Subcommands.Add(WorkItemCommand.Create(serviceProvider));
        rootCommand.Subcommands.Add(PrCommand.Create(serviceProvider));

        return rootCommand;
    }

    public static ParseResult Parse(string[] args, IServiceProvider serviceProvider)
    {
        return BuildRootCommand(serviceProvider).Parse(args, ParserConfiguration);
    }

    public static async Task<int> InvokeAsync(string[] args, IServiceProvider serviceProvider)
    {
        var parseResult = Parse(args, serviceProvider);

        // InvokeAsync reports parse failures only, as 1. Quill reports every usage error as 3.
        // A command that ran and failed reports through Environment.ExitCode, which InvokeAsync does not read.
        var invokeExitCode = await parseResult.InvokeAsync();

        if (parseResult.Errors.Count > 0)
        {
            return 3;
        }

        return invokeExitCode != 0 ? invokeExitCode : Environment.ExitCode;
    }
}
