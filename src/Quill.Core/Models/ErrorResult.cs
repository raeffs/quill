using System.Text.Json.Serialization;

namespace Quill.Core.Models;

public class ErrorResult
{
    [JsonPropertyName("error")]
    public required string Error { get; init; }

    [JsonPropertyName("code")]
    public required int Code { get; init; }
}
