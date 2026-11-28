namespace Quill.Core;

internal sealed record PersonCacheFile(int Version, Dictionary<string, PersonEntry?>? Entries);
