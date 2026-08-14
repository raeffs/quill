using System.CommandLine;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quill.AzureDevOps;
using Quill.Cli;
using Quill.Core.Markdown;
using Quill.Core.Models;
using Quill.Core.Validation;

namespace Quill.Cli.Commands.Wi;

internal static class CreateCommand
{
    private static readonly string[] AllowedTypes = ["Product Backlog Item", "Bug"];

    public static Command Create(IServiceProvider serviceProvider)
    {
        var fileArg = new Argument<string>("file-path") { Description = "Path to the markdown file with frontmatter" };
        var assignedToOption = new Option<string?>("--assigned-to") { Description = "Name of the user to assign the work item to. Defaults to the authenticated PAT user." };

        var command = new Command("create", "Create a new work item on Azure DevOps from a markdown file")
        {
            fileArg,
            assignedToOption,
        };

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var filePath = parseResult.GetValue(fileArg)!;
            var assignedTo = parseResult.GetValue(assignedToOption);
            await ExecuteAsync(serviceProvider, filePath, assignedTo);
        });

        return command;
    }

    private static async Task ExecuteAsync(IServiceProvider serviceProvider, string filePath, string? assignedTo)
    {
        try
        {
            var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Quill.Create");

            if (!File.Exists(filePath))
            {
                Console.WriteLine(JsonSerializer.Serialize(
                    new ErrorResult { Error = $"File not found: {filePath}", Code = 3 }, CommandHelpers.Context.ErrorResult));
                Environment.ExitCode = 3;
                return;
            }

            var config = serviceProvider.GetRequiredService<QuillConfig>();
            var content = await File.ReadAllTextAsync(filePath);
            var parsed = FrontmatterParser.Parse(content);

            if (parsed.Id != 0)
            {
                Console.WriteLine(JsonSerializer.Serialize(
                    new ErrorResult { Error = "Work item id must be 0 for creation.", Code = 1 }, CommandHelpers.Context.ErrorResult));
                Environment.ExitCode = 1;
                return;
            }

            if (string.IsNullOrWhiteSpace(parsed.Title))
            {
                Console.WriteLine(JsonSerializer.Serialize(
                    new ErrorResult { Error = "Frontmatter must contain a non-empty 'title' field.", Code = 1 }, CommandHelpers.Context.ErrorResult));
                Environment.ExitCode = 1;
                return;
            }

            if (string.IsNullOrWhiteSpace(parsed.Type))
            {
                Console.WriteLine(JsonSerializer.Serialize(
                    new ErrorResult { Error = "Frontmatter must contain a 'type' field.", Code = 1 }, CommandHelpers.Context.ErrorResult));
                Environment.ExitCode = 1;
                return;
            }

            if (!AllowedTypes.Contains(parsed.Type, StringComparer.OrdinalIgnoreCase))
            {
                Console.WriteLine(JsonSerializer.Serialize(
                    new ErrorResult { Error = $"Type must be one of: {string.Join(", ", AllowedTypes)}.", Code = 1 }, CommandHelpers.Context.ErrorResult));
                Environment.ExitCode = 1;
                return;
            }

            if (parsed.ParentId is null or 0)
            {
                Console.WriteLine(JsonSerializer.Serialize(
                    new ErrorResult { Error = "Frontmatter must contain a non-zero 'parentId' field.", Code = 1 }, CommandHelpers.Context.ErrorResult));
                Environment.ExitCode = 1;
                return;
            }

            var client = serviceProvider.GetRequiredService<AzureDevOpsClient>();
            var identityClient = serviceProvider.GetRequiredService<AzureDevOpsIdentityClient>();

            var parentWorkItem = await client.GetWorkItemAsync(parsed.ParentId.Value);
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

            var assignee = string.IsNullOrWhiteSpace(assignedTo) ? currentUser.DisplayName : assignedTo;

            var (rawHtml, linkIds) = await MarkdownConverter.ToHtmlAsync(
                parsed.Body, config.ServerUrl, config.Collection, config.Project, client, logger);

            var styledHtml = HtmlStyler.ApplyStyles(rawHtml);
            var sanitizer = new Ganss.Xss.HtmlSanitizer();
            sanitizer.AllowedAttributes.Add("style");
            sanitizer.AllowedAttributes.Add("data-vss-mention");
            sanitizer.AllowedAttributes.Add("data-lang");
            var descriptionHtml = sanitizer.Sanitize(styledHtml);

            var newId = await client.CreateWorkItemAsync(
                parsed.Type,
                parsed.Title,
                parsed.ParentId.Value,
                assignee,
                string.IsNullOrEmpty(descriptionHtml) ? null : descriptionHtml,
                parentWorkItem.IterationPath);

            foreach (var targetId in linkIds)
            {
                await client.AddRelationAsync(newId, targetId);
            }

            var updatedContent = FrontmatterParser.Write(
                id: newId,
                type: parsed.Type,
                title: parsed.Title,
                state: "New",
                body: parsed.Body.TrimEnd(),
                parentId: parsed.ParentId);

            await File.WriteAllTextAsync(filePath, updatedContent);

            Console.WriteLine(JsonSerializer.Serialize(
                new CreateResult
                {
                    Id = newId,
                    Title = parsed.Title,
                },
                CommandHelpers.Context.CreateResult));
        }
        catch (Exception ex) when (ex is FileNotFoundException or InvalidOperationException or HttpRequestException)
        {
            CommandHelpers.HandleError(ex);
        }
    }
}
