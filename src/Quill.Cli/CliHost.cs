using System.CommandLine;
using System.CommandLine.Parsing;
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

        rootCommand.Subcommands.Add(WorkItemCommand.Create(serviceProvider));
        rootCommand.Subcommands.Add(PrCommand.Create(serviceProvider));

        return rootCommand;
    }

    public static ParseResult Parse(string[] args, IServiceProvider serviceProvider)
    {
        return BuildRootCommand(serviceProvider).Parse(args, ParserConfiguration);
    }
}
