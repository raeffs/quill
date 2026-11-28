using System.Text.Json.Serialization;

namespace Quill.Core;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(PersonCacheFile))]
internal sealed partial class PersonCacheJsonContext : JsonSerializerContext
{
}
