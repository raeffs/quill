using System.CommandLine;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Quill.AzureDevOps;
using Quill.Cli;
using Quill.Core.Models;
using Quill.Core.Validation;

namespace Quill.Cli.Commands.Wi;

internal static class CreateTaskCommand
{
    public static Command Create(IServiceProvider serviceProvider)
    {
        var parentIdArg = new Argument<int>("parent-id") { Description = "The parent work item ID" };
        var titleArg = new Argument<string>("title") { Description = "Title for the new task" };

        var command = new Command("create-task", "Create a new task as a child of a PBI or Bug")
        {
            parentIdArg,
            titleArg,
        };

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var parentId = parseResult.GetValue(parentIdArg);
            var title = parseResult.GetValue(titleArg)!;
            await ExecuteAsync(serviceProvider, parentId, title);
        });

        return command;
    }

    private static async Task ExecuteAsync(IServiceProvider serviceProvider, int parentId, string title)
    {
        try
        {
            var config = serviceProvider.GetRequiredService<QuillConfig>();
            var client = serviceProvider.GetRequiredService<AzureDevOpsClient>();
            var identityClient = serviceProvider.GetRequiredService<AzureDevOpsIdentityClient>();

            var parentWorkItem = await client.GetWorkItemAsync(parentId);
            var currentUser = await identityClient.GetCurrentUserAsync();

            var validation = ParentValidator.Validate(parentWorkItem, config, currentUser.Id);

            if (!validation.IsValid)
            {
                var errorMsg = string.Join("; ", validation.Errors);
                Console.WriteLine(JsonSerializer.Serialize(
                    new ErrorResult { Error = errorMsg, Code = 1 }, CommandHelpers.Context.ErrorResult));
                Environment.ExitCode = 1;
                return;
            }

            var newId = await client.CreateWorkItemAsync(
                "Task", title, parentId, iterationPath: parentWorkItem.IterationPath);

            Console.WriteLine(JsonSerializer.Serialize(
                new CreateResult
                {
                    Id = newId,
                    Title = title,
                },
                CommandHelpers.Context.CreateResult));
        }
        catch (Exception ex) when (ex is FileNotFoundException or InvalidOperationException or HttpRequestException)
        {
            CommandHelpers.HandleError(ex);
        }
    }
}
