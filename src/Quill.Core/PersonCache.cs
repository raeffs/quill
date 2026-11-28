using Quill.Core.Models;

namespace Quill.Core;

public sealed class PersonCache(IPersonStore store, PersonCacheScope scope, TimeProvider clock)
{
    public static readonly TimeSpan MaxAge = TimeSpan.FromHours(24);

    private IReadOnlyDictionary<string, PersonEntry>? _entries;

    public Person? Find(string name)
    {
        var entry = Get(PersonKey(name));
        return entry is null ? null : new Person(entry.Id, entry.DisplayName, entry.UniqueName);
    }

    public void Remember(string name, Person person) =>
        Add(PersonKey(name), new PersonEntry(person.Id, person.DisplayName, person.UniqueName, clock.GetUtcNow()));

    public CurrentUser? FindMe()
    {
        var entry = Get(MeKey());
        return entry is null ? null : new CurrentUser(entry.Id, entry.DisplayName);
    }

    public void RememberMe(CurrentUser me) =>
        Add(MeKey(), new PersonEntry(me.Id, me.DisplayName, null, clock.GetUtcNow()));

    private string PersonKey(string name) => $"{scope.ServerUrl}|{scope.Collection}|person|{name.Trim().ToUpperInvariant()}";

    private string MeKey() => $"{scope.ServerUrl}|{scope.Collection}|me|{scope.PatHash}";

    private bool IsValid(PersonEntry entry)
    {
        var now = clock.GetUtcNow();
        return entry.SavedAt <= now && now - entry.SavedAt < MaxAge;
    }

    private PersonEntry? Get(string key)
    {
        _entries ??= store.Load();
        return _entries.TryGetValue(key, out var entry) && IsValid(entry) ? entry : null;
    }

    private void Add(string key, PersonEntry entry)
    {
        var current = store.Load()
            .Where(pair => IsValid(pair.Value))
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
        current[key] = entry;
        store.Save(current);
        _entries = current;
    }
}
