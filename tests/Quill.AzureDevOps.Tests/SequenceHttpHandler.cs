using System.Net;
using System.Text;

namespace Quill.AzureDevOps.Tests;

internal sealed class SequenceHttpHandler : HttpMessageHandler
{
    private readonly Queue<(HttpStatusCode Status, string Body)> _answers;

    public SequenceHttpHandler(params (HttpStatusCode Status, string Body)[] answers)
    {
        _answers = new Queue<(HttpStatusCode, string)>(answers);
    }

    public List<HttpRequestMessage> Requests { get; } = [];

    public List<string> Bodies { get; } = [];

    public List<string?> ContentTypes { get; } = [];

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        Requests.Add(request);
        Bodies.Add(request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken));
        ContentTypes.Add(request.Content?.Headers.ContentType?.MediaType);

        var (status, body) = _answers.Dequeue();
        return new HttpResponseMessage(status)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };
    }
}
