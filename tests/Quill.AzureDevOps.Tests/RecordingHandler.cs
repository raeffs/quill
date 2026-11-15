namespace Quill.AzureDevOps.Tests;

internal sealed class RecordingHandler : HttpMessageHandler
{
    private readonly Queue<FakeResponse> _responses;

    public RecordingHandler(IEnumerable<FakeResponse> responses)
    {
        _responses = new Queue<FakeResponse>(responses);
    }

    public List<RecordedRequest> Requests { get; } = new();

    public List<string> RequestBodies { get; } = new();

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(new RecordedRequest(request.Method, request.RequestUri?.ToString()));
        RequestBodies.Add(request.Content is not null
            ? await request.Content.ReadAsStringAsync(cancellationToken)
            : string.Empty);

        if (_responses.Count == 0)
        {
            throw new InvalidOperationException("RecordingHandler ran out of scripted responses.");
        }

        var next = _responses.Dequeue();
        var response = new HttpResponseMessage(next.Status)
        {
            Content = new StringContent(next.Body, System.Text.Encoding.UTF8, "application/json"),
        };
        if (next.RetryAfterSeconds is int secs)
        {
            response.Headers.Add("Retry-After", secs.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        return response;
    }
}
