using System.Text.Json.Serialization;
using Quill.Core.Models;

namespace Quill.Core.Config;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(QuillConfig))]
internal sealed partial class ConfigJsonContext : JsonSerializerContext
{
}
