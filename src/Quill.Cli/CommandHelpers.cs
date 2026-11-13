using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Quill.Core;
using Quill.Core.Models;

namespace Quill.Cli;

internal static class CommandHelpers
{
    public const string DefectAdvice = "This is a bug in quill. Report it at https://github.com/raeffs/quill/issues and attach the text quill wrote to stderr.";

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
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
#pragma warning disable CA1031 // The frame turns every failure into one error row
        catch (Exception ex)
        {
            HandleError(ex);
        }
#pragma warning restore CA1031
    }

    public static void HandleError(Exception ex)
    {
        var (message, code) = ex switch
        {
            DefectException => Defect(ex),
            QuillException quill => (ex.Message, quill.ExitCode),
            FileNotFoundException => (ex.Message, 3),
            InvalidOperationException => (ex.Message, 3),
            InvalidRequestException => (ex.Message, 1),
            _ => Defect(ex),
        };

        Console.WriteLine(JsonSerializer.Serialize(
            new ErrorResult { Error = message, Code = code }, Context.ErrorResult));
        Environment.ExitCode = code;
    }

    private static (string Message, int Code) Defect(Exception ex)
    {
        var detail = ex is DefectException ? ex.Message : $"{ex.GetType().Name}: {ex.Message}";
        var version = typeof(CommandHelpers).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        Console.Error.WriteLine($"quill {version}");
        Console.Error.WriteLine(ex.ToString());
        return ($"{detail} {DefectAdvice}", 4);
    }
}
