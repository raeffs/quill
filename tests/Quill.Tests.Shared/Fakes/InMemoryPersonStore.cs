using Quill.Core;

namespace Quill.Tests.Shared.Fakes;

public sealed class InMemoryPersonStore : IPersonStore
{
#pragma warning disable CA1002, MA0016 // Tests assert on the saved entries as a dictionary
    public Dictionary<string, PersonEntry> Entries { get; private set; } = new(StringComparer.Ordinal);
#pragma warning restore CA1002, MA0016

    public int Loads { get; private set; }

    public int Saves { get; private set; }

    public IReadOnlyDictionary<string, PersonEntry> Load()
    {
        Loads++;
        return new Dictionary<string, PersonEntry>(Entries, StringComparer.Ordinal);
    }

    public void Save(IReadOnlyDictionary<string, PersonEntry> entries)
    {
        Saves++;
        Entries = new Dictionary<string, PersonEntry>(entries, StringComparer.Ordinal);
    }
}
