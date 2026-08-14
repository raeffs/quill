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

internal static class PushCommand
{
    public static Command Create(IServiceProvider serviceProvider)
    {
        var fileArg = new Argument<string>("file-path") { Description = "Path to the markdown file to push" };

        var command = new Command("push", "Push a local markdown file to Azure DevOps")
        {
            fileArg,
        };

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var filePath = parseResult.GetValue(fileArg)!;
            await ExecuteAsync(serviceProvider, filePath);
        });

        return command;
    }

    private static async Task ExecuteAsync(IServiceProvider serviceProvider, string filePath)
    {
        try
        {
            var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Quill.Push");

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

            var client = serviceProvider.GetRequiredService<AzureDevOpsClient>();
            var identityClient = serviceProvider.GetRequiredService<AzureDevOpsIdentityClient>();

            var workItem = await client.GetWorkItemAsync(parsed.Id);
            var currentUser = await identityClient.GetCurrentUserAsync();

            var validation = PushValidator.Validate(workItem, config, currentUser.Id);

            if (!validation.IsValid)
            {
                var errorMsg = string.Join("; ", validation.Errors);
                Console.WriteLine(JsonSerializer.Serialize(
                    new ErrorResult { Error = errorMsg, Code = 1 }, CommandHelpers.Context.ErrorResult));
                Environment.ExitCode = 1;
                return;
            }

            var (rawHtml, linkIds) = await MarkdownConverter.ToHtmlAsync(
                parsed.Body, config.ServerUrl, config.Collection, config.Project, client, logger);

            var styledHtml = HtmlStyler.ApplyStyles(rawHtml);
            var sanitizer = new Ganss.Xss.HtmlSanitizer();
            sanitizer.AllowedAttributes.Add("style");
            sanitizer.AllowedAttributes.Add("data-vss-mention");
            sanitizer.AllowedAttributes.Add("data-lang");
            var html = sanitizer.Sanitize(styledHtml);

            await client.UpdateWorkItemFieldsAsync(parsed.Id, workItem.Type, parsed.Title, html);

            var existingRelatedIds = workItem.Relations
                .Where(r => string.Equals(r.RelationType, AzureDevOpsConstants.RelatedLinkType, StringComparison.Ordinal))
                .Select(r => r.TargetId)
                .ToHashSet();

            var newRelations = linkIds.Where(id => !existingRelatedIds.Contains(id)).ToList();
            foreach (var targetId in newRelations)
            {
                await client.AddRelationAsync(parsed.Id, targetId);
            }

            Console.WriteLine(JsonSerializer.Serialize(
                new PushResult
                {
                    Id = parsed.Id,
                    Title = parsed.Title,
                    UpdatedFields = ["title", "description"],
                    RelationsAdded = newRelations,
                },
                CommandHelpers.Context.PushResult));
        }
        catch (Exception ex) when (ex is FileNotFoundException or InvalidOperationException or HttpRequestException)
        {
            CommandHelpers.HandleError(ex);
        }
    }
}
