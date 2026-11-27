using System.Text.Json.Serialization;
using Quill.AzureDevOps.Dto;

namespace Quill.AzureDevOps.Tests;

[JsonSerializable(typeof(WorkItemResponse))]
[JsonSerializable(typeof(WiqlQueryRequest))]
internal sealed partial class ServerTestJsonContext : JsonSerializerContext
{
}
