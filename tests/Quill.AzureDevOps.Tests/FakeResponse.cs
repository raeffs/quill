using System.Net;

namespace Quill.AzureDevOps.Tests;

internal sealed record FakeResponse(HttpStatusCode Status, string Body, int? RetryAfterSeconds = null);
