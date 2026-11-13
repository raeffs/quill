using System.Globalization;
using Quill.Core;

namespace Quill.AzureDevOps;

public sealed class FailureModeHandler(TimeSpan timeout) : DelegatingHandler
{
    public static readonly HttpRequestOptionsKey<Func<ServerError, bool>> RejectedWhen = new("quill.rejected-when");

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(timeout);

        HttpResponseMessage? response = null;
        try
        {
            response = await base.SendAsync(request, timeoutSource.Token).ConfigureAwait(false);
            await response.Content.LoadIntoBufferAsync(timeoutSource.Token).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                return response;
            }

            throw await ToFailureAsync(request, response).ConfigureAwait(false);
        }
        catch (HttpRequestException ex) when (cancellationToken.IsCancellationRequested)
        {
            response?.Dispose();
            throw new OperationCanceledException(ex.Message, ex, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            response?.Dispose();
            throw new UnavailableException($"No answer from the server: {ex.Message}", ex);
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested && timeoutSource.IsCancellationRequested)
        {
            response?.Dispose();
            var seconds = timeout.TotalSeconds.ToString(CultureInfo.InvariantCulture);
            throw new UnavailableException($"The server did not answer within {seconds} seconds.", ex);
        }
        catch
        {
            response?.Dispose();
            throw;
        }
    }

    private static async Task<QuillException> ToFailureAsync(HttpRequestMessage request, HttpResponseMessage response)
    {
        var status = (int)response.StatusCode;
        var body = await response.Content.ReadAsStringAsync(CancellationToken.None).ConfigureAwait(false);
        var message = ServerErrorMessage.Read(body);
        var serverError = new ServerError(status, message, ServerErrorMessage.ReadTypeKey(body));
        var reason = string.IsNullOrEmpty(response.ReasonPhrase) ? string.Empty : $" ({response.ReasonPhrase})";
        var text = message is null
            ? $"The server answered {status}{reason}."
            : $"{message} ({status})";

        if (request.Options.TryGetValue(RejectedWhen, out var rejectedWhen) && rejectedWhen(serverError))
        {
            return new RejectedException(text);
        }

        return status switch
        {
            400 or 404 or 409 or 422 => new RejectedException(text),
            401 or 403 => new DeniedException(text),
            408 or 429 or >= 500 and < 600 => new UnavailableException(text, RetryAfterHeader.Read(response)),
            _ => new DefectException(text),
        };
    }
}
