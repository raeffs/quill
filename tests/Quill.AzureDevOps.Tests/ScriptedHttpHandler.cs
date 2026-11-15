using System.Net;

namespace Quill.AzureDevOps.Tests;

internal class ScriptedHttpHandler : HttpMessageHandler
{
    private readonly Queue<string> _responses;

    public ScriptedHttpHandler(IEnumerable<string> responses)
    {
        _responses = new Queue<string>(responses);
    }

    public List<HttpRequestMessage> Requests { get; } = new();

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        var body = _responses.Count > 0 ? _responses.Dequeue() : "{}";
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json"),
        });
    }
}
