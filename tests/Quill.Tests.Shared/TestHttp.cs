using Quill.AzureDevOps;

namespace Quill.Tests.Shared;

public static class TestHttp
{
    public static HttpClient Client(HttpMessageHandler inner, TimeSpan? timeout = null)
    {
#pragma warning disable CA2000 // The returned client disposes the handler
        var handler = new FailureModeHandler(timeout ?? TimeSpan.FromSeconds(30)) { InnerHandler = inner };
        return new HttpClient(handler) { Timeout = Timeout.InfiniteTimeSpan };
#pragma warning restore CA2000
    }
}
