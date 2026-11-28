using Quill.Core.Models;

namespace Quill.Core;

public sealed class PersonCache(IPersonStore store, PersonCacheScope scope, TimeProvider clock)
{
    public static readonly TimeSpan MaxAge = TimeSpan.FromHours(24);

    private IReadOnlyDictionary<string, PersonEntry>? _entries;

    public Identity? Find(string name, bool acceptGroups)
    {
        var entry = Get(NameKey(name, acceptGroups));
        return entry is null ? null : new Identity(entry.Id, entry.DisplayName, entry.UniqueName, entry.IsGroup);
    }

    public void Remember(string name, bool acceptGroups, Identity identity) =>
        Add(
            NameKey(name, acceptGroups),
            new PersonEntry(identity.Id, identity.DisplayName, identity.UniqueName, identity.IsGroup, clock.GetUtcNow()));

    public CurrentUser? FindMe()
    {
        var entry = Get(MeKey());
        return entry is null ? null : new CurrentUser(entry.Id, entry.DisplayName);
    }

    public void RememberMe(CurrentUser me) =>
        Add(MeKey(), new PersonEntry(me.Id, me.DisplayName, null, false, clock.GetUtcNow()));

    private string NameKey(string name, bool acceptGroups) =>
        $"{scope.ServerUrl}|{scope.Collection}|{(acceptGroups ? "identity" : "person")}|{name.Trim().ToUpperInvariant()}";

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
