using System.Net;
using System.Text;

namespace Quill.AzureDevOps.Tests;

internal sealed class ScriptedCreateHandler : HttpMessageHandler
{
    private readonly Queue<string> _responses;

    public ScriptedCreateHandler(IEnumerable<string> responses)
    {
        _responses = new Queue<string>(responses);
    }

    public List<RecordedRequest> Requests { get; } = new();

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(new RecordedRequest(
            request.Method,
            request.RequestUri?.ToString(),
            request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken)));

        var body = _responses.Count > 0 ? _responses.Dequeue() : "{}";
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };
    }
}
