using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Quill.Core;

namespace Quill.AzureDevOps;

public sealed class AzureDevOpsServer(HttpClient httpClient, string serverUrl, string collection, string project)
{
    private readonly string _baseUrl = serverUrl.TrimEnd('/');

#pragma warning disable CA1055 // Clients and tests compare and build URLs as text
    public string UrlOf(ServerScope scope, string path) => scope switch
    {
        ServerScope.Collection => $"{_baseUrl}/{collection}/{path}",
        ServerScope.Project => $"{_baseUrl}/{collection}/{project}/{path}",
        _ => throw new ArgumentOutOfRangeException(nameof(scope), scope, null),
    };
#pragma warning restore CA1055

    public async Task<T> ReadAsync<T>(ServerRequest request, JsonTypeInfo<T> answerType, CancellationToken cancellationToken)
        where T : class
    {
        var answer = await ReadOrNothingAsync(request, answerType, cancellationToken).ConfigureAwait(false);
        return answer ?? throw Unreadable(request, "the body is empty.", null);
    }

    public async Task<T?> ReadOrNothingAsync<T>(ServerRequest request, JsonTypeInfo<T> answerType, CancellationToken cancellationToken)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(answerType);

        using var response = await SendCoreAsync(request, cancellationToken).ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.NoContent)
        {
            return null;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(body))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize(body, answerType);
        }
        catch (JsonException ex)
        {
            throw Unreadable(request, IsJson(body) ? ex.Message : "the body is not valid JSON.", ex);
        }
    }

    public async Task SendAsync(ServerRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        using var response = await SendCoreAsync(request, cancellationToken).ConfigureAwait(false);
    }

    private static bool IsJson(string body)
    {
        try
        {
            using var document = JsonDocument.Parse(body);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static DefectException Unreadable(ServerRequest request, string reason, Exception? inner)
    {
        var separator = request.Path.IndexOf('?', StringComparison.Ordinal);
        var path = separator < 0 ? request.Path : request.Path[..separator];
        var message = $"Cannot read the answer to {request.Method.Method} {path}: {reason}";
        return inner is null ? new DefectException(message) : new DefectException(message, inner);
    }

    private async Task<HttpResponseMessage> SendCoreAsync(ServerRequest request, CancellationToken cancellationToken)
    {
        var url = UrlOf(request.Scope, request.Path);
        if (request.ApiVersion is not null)
        {
            var separator = url.Contains('?', StringComparison.Ordinal) ? '&' : '?';
            url = $"{url}{separator}api-version={request.ApiVersion}";
        }

        using var message = new HttpRequestMessage(request.Method, url);
        message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        if (request.Body is not null)
        {
            message.Content = new StringContent(request.Body, Encoding.UTF8, request.ContentType!);
        }

        if (request.RejectedWhen is not null)
        {
            message.Options.Set(FailureModeHandler.RejectedWhen, request.RejectedWhen);
        }

        return await httpClient.SendAsync(message, cancellationToken).ConfigureAwait(false);
    }
}
