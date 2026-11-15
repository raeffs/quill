namespace Quill.AzureDevOps.Tests;

internal sealed record RecordedRequest(HttpMethod Method, string? Url, string? Body = null);
