using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Quill.AzureDevOps.Dto;

namespace Quill.AzureDevOps;

public sealed record ServerRequest
{
    private const string JsonContentType = "application/json";
    private const string JsonPatchContentType = "application/json-patch+json";

    private ServerRequest(HttpMethod method, ServerScope scope, string path, string? body, string? contentType)
    {
        Method = method;
        Scope = scope;
        Path = path;
        Body = body;
        ContentType = contentType;
    }

    public string? ApiVersion { get; init; } = AzureDevOpsConstants.ApiVersion;

    public Func<ServerError, bool>? RejectedWhen { get; init; }

    internal HttpMethod Method { get; }

    internal ServerScope Scope { get; }

    internal string Path { get; }

    internal string? Body { get; }

    internal string? ContentType { get; }

    public static ServerRequest Get(ServerScope scope, string path) =>
        new(HttpMethod.Get, scope, path, null, null);

    public static ServerRequest Post<TBody>(ServerScope scope, string path, TBody body, JsonTypeInfo<TBody> bodyType) =>
        new(HttpMethod.Post, scope, path, JsonSerializer.Serialize(body, bodyType), JsonContentType);

    public static ServerRequest Patch<TBody>(ServerScope scope, string path, TBody body, JsonTypeInfo<TBody> bodyType) =>
        new(HttpMethod.Patch, scope, path, JsonSerializer.Serialize(body, bodyType), JsonContentType);

#pragma warning disable CA1002, MA0016 // The JSON patch context serialises List<JsonPatchOperation>
    public static ServerRequest JsonPatch(HttpMethod method, ServerScope scope, string path, List<JsonPatchOperation> operations) =>
        new(method, scope, path, JsonSerializer.Serialize(operations, AzureDevOpsJsonContext.Default.ListJsonPatchOperation), JsonPatchContentType);
#pragma warning restore CA1002, MA0016
}
