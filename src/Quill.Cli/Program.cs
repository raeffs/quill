using Microsoft.Extensions.DependencyInjection;
using Quill.Cli;

var services = new ServiceCollection();
services.AddQuillServices();
await using var provider = services.BuildServiceProvider();

return await CliHost.InvokeAsync(args, provider);
