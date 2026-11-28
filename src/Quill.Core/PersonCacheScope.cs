using System.Security.Cryptography;
using System.Text;

namespace Quill.Core;

public sealed record PersonCacheScope(string ServerUrl, string Collection, string PatHash)
{
    public static PersonCacheScope For(string serverUrl, string collection, string pat) => new(
        serverUrl.Trim().TrimEnd('/').ToUpperInvariant(),
        collection.Trim().ToUpperInvariant(),
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(pat))));
}
