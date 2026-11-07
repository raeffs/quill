using System.Diagnostics;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Quill.Core;
using Quill.Core.Models;

namespace Quill.Cli;

internal static class CommandHelpers
{
    // The encoder is why this is a context instance rather than CliJsonContext.Default:
    // [JsonSourceGenerationOptions] cannot express an encoder, and the default one
    // escapes <, >, &, + and every non-ASCII character. Work item titles are full of those.
    public static readonly CliJsonContext Context = new CliJsonContext(new JsonSerializerOptions
    {
        WriteIndented = false,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    });

    public static async Task RunAsync<TRow>(
        Func<CancellationToken, Task<TRow>> action,
        JsonTypeInfo<TRow> typeInfo,
        CancellationToken cancellationToken)
    {
        try
        {
            var row = await action(cancellationToken);
            Console.WriteLine(JsonSerializer.Serialize(row, typeInfo));
        }
        catch (Exception ex) when (ex is FileNotFoundException or InvalidOperationException or HttpRequestException or InvalidRequestException)
        {
            HandleError(ex);
        }
    }

    public static void HandleError(Exception ex)
    {
        var (message, code) = ex switch
        {
            FileNotFoundException => (ex.Message, 3),
            InvalidOperationException => (ex.Message, 3),
            WorkItemRejectedException => (ex.Message, 1),
            PipelineRejectedException => (ex.Message, 1),
            InvalidRequestException => (ex.Message, 1),
            HttpRequestException => ($"API error: {ex.Message}", 2),
            _ => throw new UnreachableException(ex.GetType().Name, ex),
        };

        Console.WriteLine(JsonSerializer.Serialize(
            new ErrorResult { Error = message, Code = code }, Context.ErrorResult));
        Environment.ExitCode = code;
    }
}
