using Microsoft.Extensions.DependencyInjection;
using Quill.Cli;

var services = new ServiceCollection();
services.AddQuillServices();
await using var provider = services.BuildServiceProvider();

// InvokeAsync reports parse failures only. A command that ran and failed reports through
// Environment.ExitCode, which InvokeAsync does not read.
var parseExitCode = await CliHost.Parse(args, provider).InvokeAsync();

return parseExitCode != 0 ? parseExitCode : Environment.ExitCode;
