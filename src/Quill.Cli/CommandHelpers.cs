using System.Text.Encodings.Web;
using System.Text.Json;
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

    public static void HandleError(Exception ex)
    {
        var (message, code) = ex switch
        {
            FileNotFoundException => (ex.Message, 3),
            InvalidOperationException => (ex.Message, 3),
            HttpRequestException => ($"API error: {ex.Message}", 2),
            _ => (ex.Message, 1),
        };

        Console.WriteLine(JsonSerializer.Serialize(
            new ErrorResult { Error = message, Code = code }, Context.ErrorResult));
        Environment.ExitCode = code;
    }
}
