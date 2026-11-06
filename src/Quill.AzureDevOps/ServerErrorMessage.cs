using System.Text.Json;
using Quill.AzureDevOps.Dto;

namespace Quill.AzureDevOps;

internal static class ServerErrorMessage
{
    public static string? Read(string body)
    {
        var dto = Parse(body);
        return string.IsNullOrWhiteSpace(dto?.Message) ? null : dto.Message.ReplaceLineEndings(" ");
    }

    public static string? ReadTypeKey(string body)
    {
        return Parse(body)?.TypeKey;
    }

    private static ApiErrorResponse? Parse(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize(body, AzureDevOpsJsonContext.Default.ApiErrorResponse);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
