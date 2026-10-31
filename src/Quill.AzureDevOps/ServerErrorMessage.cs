using System.Text.Json;

namespace Quill.AzureDevOps;

internal static class ServerErrorMessage
{
    public static string? Read(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return null;
        }

        try
        {
            var dto = JsonSerializer.Deserialize(body, AzureDevOpsJsonContext.Default.ApiErrorResponse);
            return string.IsNullOrWhiteSpace(dto?.Message) ? null : dto.Message.ReplaceLineEndings(" ");
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
